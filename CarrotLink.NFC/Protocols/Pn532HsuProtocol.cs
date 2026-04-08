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

        if (nfcPacket.CommandDefinition != null)
        {
            body.Add(nfcPacket.CommandDefinition.OpCode);
            foreach (var descriptor in nfcPacket.CommandDefinition.GetDescriptors())
            {
                body.AddRange(descriptor.Value.ToArray());
            }
        }
        else
        {
            // 如果没有定义，回退到原始解析方式（保持兼容性）
            switch (nfcPacket.Action)
            {
                case NfcAction.ListPassiveTarget:
                    body.AddRange(new byte[] { 0x4A, 0x01, 0x00 });
                    break;
                case NfcAction.REQA:
                    body.AddRange(new byte[] { 0x42, 0x26 });
                    break;
                case NfcAction.WUPA:
                    body.AddRange(new byte[] { 0x42, 0x52 });
                    break;
                case NfcAction.Halt:
                    body.AddRange(new byte[] { 0x42, 0x50, 0x00 });
                    break;
            }
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

        // 更新消耗后的缓冲区指针
        var frameEndPosition = reader.Position;
        var consumedBuffer = buffer.Slice(0, frameEndPosition);
        buffer = buffer.Slice(frameEndPosition);

        // 7. 解析 PN532 响应包 (TFI = 0xD5)
        if (data.Length > 0 && data[0] == 0xD5)
        {
            byte opCode = data.Length > 1 ? data[1] : (byte)0;
            // 响应载荷基准 Memory（用于零拷贝分割）
            // 注意：我们从原始 buffer 中切分出响应字段，而不是使用复制的 data 数组
            // 找到 data[2] 在原始 buffer 中的位置（跳过 00 00 FF LEN LCS D5 OpCode）
            var payloadMemory = consumedBuffer.Slice(consumedBuffer.GetPosition(7)).ToArray().AsMemory(); 
            // 简单处理：如果是从 ReadOnlySequence 直接切分比较复杂，先转为 Memory 处理响应字段
            // 以后可以进一步优化为直接在 ReadOnlySequence 上切分

            var result = new NfcPacket
            {
                IsSuccess = true,
                Action = NfcAction.Response,
                Mnemonic = $"PN532.{opCode:X2}"
            };

            // 使用解释器自动分割字段
            var (action, responseFields) = Pn532Interpreter.Interpret(opCode, payloadMemory);
            
            packet = result with 
            { 
                Action = action,
                ResponseFields = responseFields 
            };
            return true;
        }

        return false;
    }

    /// <summary>
    /// PN532 响应解析器，将原始载荷切分为结构化字段。
    /// </summary>
    private static class Pn532Interpreter
    {
        public static (NfcAction Action, List<NfcFieldDescriptor> Fields) Interpret(byte opCode, ReadOnlyMemory<byte> payload)
        {
            var fields = new List<NfcFieldDescriptor>();
            NfcAction action = NfcAction.Response;

            try
            {
                switch (opCode)
                {
                    case 0x4B: // InListPassiveTarget Res
                        action = NfcAction.GetAtqa;
                        if (payload.Length >= 1)
                        {
                            fields.Add(new NfcFieldDescriptor("NbTarget", payload.Slice(0, 1)));
                            if (payload.Span[0] > 0 && payload.Length >= 6)
                            {
                                fields.Add(new NfcFieldDescriptor("Tg", payload.Slice(1, 1)));
                                fields.Add(new NfcFieldDescriptor("SENS_RES", payload.Slice(2, 2), "ATQA"));
                                fields.Add(new NfcFieldDescriptor("SEL_RES", payload.Slice(4, 1), "SAK"));
                                fields.Add(new NfcFieldDescriptor("NFCIDLength", payload.Slice(5, 1)));
                                int uidLen = payload.Span[5];
                                if (payload.Length >= 6 + uidLen)
                                {
                                    fields.Add(new NfcFieldDescriptor("NFCID", payload.Slice(6, uidLen), "UID"));
                                }
                            }
                        }
                        break;
                    case 0x41: // InDataExchange Res
                        action = NfcAction.Response;
                        if (payload.Length >= 1)
                        {
                            fields.Add(new NfcFieldDescriptor("Status", payload.Slice(0, 1)));
                            if (payload.Length > 1)
                            {
                                fields.Add(new NfcFieldDescriptor("Data", payload.Slice(1)));
                            }
                        }
                        break;
                    case 0x43: // InCommunicateThru Res
                        action = NfcAction.Response;
                        if (payload.Length >= 1)
                        {
                            fields.Add(new NfcFieldDescriptor("Status", payload.Slice(0, 1)));
                            fields.Add(new NfcFieldDescriptor("Data", payload.Slice(1)));
                        }
                        break;
                    default:
                        fields.Add(new NfcFieldDescriptor("RawPayload", payload));
                        break;
                }
            }
            catch (Exception)
            {
                // 解析失败时保留原始数据
                fields.Clear();
                fields.Add(new NfcFieldDescriptor("ParseErrorData", payload));
            }

            return (action, fields);
        }
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
