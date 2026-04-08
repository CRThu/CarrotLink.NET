using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.NFC.Models;
using System.Buffers;
using CarrotLink.Core.Utility;

namespace CarrotLink.NFC.Protocols;

/// <summary>
/// 基础协议配置实现
/// </summary>
public class Pn532ProtocolConfig : ProtocolConfigBase { }

/// <summary>
/// PN532 HSU (High-Speed UART) 协议实现。
/// 负责 NfcAction 与 PN532 指令帧的相互转换。
/// </summary>
public class Pn532HsuProtocol : IProtocol
{
    public string ProtocolName => "PN532_HSU";
    public int ProtocolVersion => 1;
    public ProtocolConfigBase Config { get; } = new Pn532ProtocolConfig();

    // PN532 ACK 帧: 00 00 FF 00 FF 00
    private static readonly byte[] PN532_ACK = { 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00 };

    public byte[] Encode(IPacket packet)
    {
        if (packet is not NfcPacket nfcPacket) return Array.Empty<byte>();

        List<byte> body = new List<byte> { 0xD4 }; // TFI: Host to PN532

        switch (nfcPacket.Action)
        {
            case NfcAction.ListPassiveTarget:
                // InListPassiveTarget: 0x4A, MaxTargets=1, Brty=0(106k TypeA)
                body.Add(0x4A);
                body.Add(0x01);
                body.Add(0x00);
                break;
            case NfcAction.REQA:
                // InCommunicateThru: 0x42, Data=26 (7-bit REQA)
                body.Add(0x42);
                body.Add(0x26);
                break;
            case NfcAction.WUPA:
                // InCommunicateThru: 0x42, Data=52 (7-bit WUPA)
                body.Add(0x42);
                body.Add(0x52);
                break;
            case NfcAction.Halt:
                // InCommunicateThru: 0x42, Data=50 00 (HALT)
                body.Add(0x42);
                body.Add(0x50);
                body.Add(0x00);
                break;
            case NfcAction.Transceive:
                // 全透传模式
                if (nfcPacket.Payload != null) body.AddRange(nfcPacket.Payload);
                break;
            default:
                // 其他情况尝试根据现有 Payload 发送
                if (nfcPacket.Payload != null) body.AddRange(nfcPacket.Payload);
                break;
        }

        return BuildFrame(body.ToArray());
    }

    public bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
    {
        packet = null;

        // 1. 过滤 ACK (静默消耗)
        while (buffer.Length >= 6 && IsAck(buffer))
        {
            buffer = buffer.Slice(6);
        }

        if (buffer.Length < 6) return false;

        // 2. 寻找帧头 (00 00 FF)
        SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
        if (!FindFrameStart(ref reader)) return false;

        // 3. 读取长度与 LCS
        if (!reader.TryRead(out byte len) || !reader.TryRead(out byte lcs)) return false;
        if ((byte)(len + lcs) != 0) return false; // 长度校验失败

        // 4. 读取数据载荷
        byte[] data = new byte[len];
        if (!reader.TryCopyTo(data)) return false;
        reader.Advance(len);

        // 5. 读取 DCS 并校验
        if (!reader.TryRead(out byte dcs)) return false;
        if (!VerifyChecksum(data, dcs)) return false;

        // 6. 消耗结束符 Postamble (00)
        reader.TryRead(out _);

        // 更新缓冲区游标
        buffer = buffer.Slice(reader.Position);

        // 7. 解析 PN532 响应包 (TFI = 0xD5)
        if (data.Length > 0 && data[0] == 0xD5)
        {
            byte opCode = data.Length > 1 ? data[1] : (byte)0;
            byte[] rawPayload = data.Skip(2).ToArray();

            var result = new NfcPacket
            {
                IsSuccess = true,
                Payload = rawPayload
            };

            // 语义映射实现
            if (opCode == 0x4B) // ListPassiveTarget 响应 (InListPassiveTarget Res)
            {
                // 解析格式: NbTargets, [TargetData...]
                if (rawPayload.Length > 0 && rawPayload[0] > 0)
                {
                    // 简化提取逻辑：剥离 NbTarget(1) 和 Tg(1)，保留 ATQA(2)+SAK(1)+UID 等特征
                    // 注：此处根据需求将整个卡片特征塞进 Payload
                    result = result with { Action = NfcAction.GetAtqa, Payload = rawPayload.Skip(1).ToArray() };
                }
                else
                {
                    result = result with { IsSuccess = false, Action = NfcAction.Response };
                }
            }
            else if (opCode == 0x43) // InCommunicateThru 响应
            {
                result = result with { Action = NfcAction.Response };
            }
            else if (opCode == (data[1] & 0x7F)) 
            {
                // 如果返回的是错误帧 (OpCode 第 7 位通常为 1，但 PN532 也有特定错误包)
                // 这里暂不深入，统一标记失败
            }

            packet = result;
            return true;
        }

        return false;
    }

    private bool IsAck(ReadOnlySequence<byte> buffer)
    {
        int i = 0;
        foreach (var memory in buffer)
        {
            var span = memory.Span;
            for (int j = 0; j < span.Length && i < 6; j++, i++)
            {
                if (span[j] != PN532_ACK[i]) return false;
            }
            if (i == 6) break;
        }
        return i == 6;
    }

    private bool FindFrameStart(ref SequenceReader<byte> reader)
    {
        while (reader.Remaining >= 3)
        {
            if (reader.TryPeek(out byte b1) && b1 == 0x00)
            {
                var temp = reader;
                temp.Advance(1);
                if (temp.TryRead(out byte b2) && b2 == 0x00 && temp.TryRead(out byte b3) && b3 == 0xFF)
                {
                    reader.Advance(3);
                    return true;
                }
            }
            reader.Advance(1);
        }
        return false;
    }

    private bool VerifyChecksum(byte[] data, byte dcs)
    {
        byte sum = 0;
        foreach (byte b in data) sum += b;
        return (byte)(sum + dcs) == 0;
    }

    private byte[] BuildFrame(byte[] body)
    {
        int len = body.Length;
        byte lcs = (byte)((~len + 1) & 0xFF); // 2's complement of len
        
        byte sum = 0;
        foreach (byte b in body) sum += b;
        byte dcs = (byte)((~sum + 1) & 0xFF); // 2's complement of sum

        byte[] frame = new byte[len + 7];
        frame[0] = 0x00;
        frame[1] = 0x00;
        frame[2] = 0xFF;
        frame[3] = (byte)len;
        frame[4] = lcs;
        Array.Copy(body, 0, frame, 5, len);
        frame[len + 5] = dcs;
        frame[len + 6] = 0x00;

        return frame;
    }
}
