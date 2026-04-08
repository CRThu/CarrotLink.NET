using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.NFC.Models;
using System.Buffers;
using CarrotLink.Core.Utility;
using System.Collections.Generic;
using System;
using System.Linq;

namespace CarrotLink.NFC.Protocols;

/// <summary>
/// 基础协议配置实现
/// </summary>
public class Pn532ProtocolConfig : ProtocolConfigBase { }

/// <summary>
/// PN532 HSU (High-Speed UART) 协议实现。
/// 实现在芯片层级 (Layer 2) 与 卡片语义层 (Layer 7) 的解耦与桥接。
/// </summary>
public class Pn532HsuProtocol : IProtocol
{
    public string ProtocolName => "PN532_HSU";
    public int ProtocolVersion => 1;
    public ProtocolConfigBase Config { get; } = new Pn532ProtocolConfig();

    public NfcCommandRegistry Registry => _registry;
    private readonly NfcCommandRegistry _registry;

    public Pn532HsuProtocol(NfcCommandRegistry registry)
    {
        _registry = registry;
    }

    // PN532 ACK 帧: 00 00 FF 00 FF 00
    private static readonly byte[] PN532_ACK = { 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00 };

    public byte[] Encode(IPacket packet)
    {
        if (packet is not NfcPacket nfcPacket) return Array.Empty<byte>();

        List<byte> body = new List<byte> { 0xD4 }; // TFI: Host to PN532
        
        var def = nfcPacket.Definition;
        if (def == null && !string.IsNullOrEmpty(nfcPacket.Mnemonic))
        {
            def = _registry.TryGetByMnemonic(nfcPacket.Mnemonic);
        }

        if (def != null)
        {
            if (def.IsSystemCommand)
            {
                // 系统指令：直接封装 OpCode
                body.Add(def.OpCode);
            }
            else
            {
                // 卡片指令：自动套壳 InDataExchange (0x40) + Tg (0x01)
                body.Add(0x40);
                body.Add(0x01);
                body.Add(def.OpCode);
            }
        }
        else if (nfcPacket.Payload != null)
        {
            // 无定义但在 Payload 中有数据，默认作为原始载荷发送
            // 此处可以判断是否需要套壳，如果不以 0xD4/0xD5 开头且非系统指令，可考虑辅助套壳
            // 简单处理：如果是手工 HEX 模式，通常包含所有字节，不在此处画蛇添足
            body.Clear(); // 覆盖默认 D4
            body.AddRange(nfcPacket.Payload);
            return BuildFrame(body.ToArray());
        }

        // 添加参数载荷
        if (nfcPacket.Payload != null)
        {
            body.AddRange(nfcPacket.Payload);
        }

        return BuildFrame(body.ToArray());
    }

    public bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
    {
        packet = null;

        // 1. 过滤物理层 ACK
        while (buffer.Length >= 6 && IsAck(buffer))
        {
            buffer = buffer.Slice(6);
        }

        if (buffer.Length < 6) return false;

        // 2. 物理成帧识别 (00 00 FF ...)
        SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
        if (!FindFrameStart(ref reader)) return false;

        if (!reader.TryRead(out byte len) || !reader.TryRead(out byte lcs)) return false;
        if ((byte)(len + lcs) != 0) return false;

        byte[] fullData = new byte[len];
        if (!reader.TryCopyTo(fullData)) return false;
        reader.Advance(len);

        if (!reader.TryRead(out byte dcs)) return false;
        if (!VerifyChecksum(fullData, dcs)) return false;
        reader.TryRead(out _); // Postamble

        // 更新消费进度
        var consumedPosition = reader.Position;
        buffer = buffer.Slice(consumedPosition);

        // 3. 解析 TFI (D5 为响应)
        if (fullData.Length == 0 || fullData[0] != 0xD5) return false;

        byte chipOpCode = fullData.Length > 1 ? fullData[1] : (byte)0;
        
        // 4. 链路层适配逻辑
        if (chipOpCode == 0x41) // InDataExchange Response
        {
            // 剥离芯片壳：D5 41 [Status] ...
            // Status == 0 表示卡片操作成功
            byte status = fullData.Length > 2 ? fullData[2] : (byte)0xFF;
            
            // 提取纯净的卡片载荷 (跳过 D5 41 Status)
            byte[] cardPayload = fullData.Skip(3).ToArray();
            
            // 尝试恢复语义：卡片响应通常第一个字节是响应码
            byte cardOpCode = cardPayload.Length > 0 ? cardPayload[0] : (byte)0;
            var definition = _registry.TryGetByOpCode(cardOpCode, NfcDirection.Response);

            var packetResult = new NfcPacket
            {
                IsSuccess = status == 0,
                Definition = definition,
                Payload = cardPayload
            };

            // 自动填充字段描述符
            if (definition != null)
            {
                packetResult.Descriptors.AddRange(_registry.Interpret(definition, cardPayload.AsMemory()));
            }

            packet = packetResult;
        }
        else
        {
            // 芯片系统指令响应
            var definition = _registry.TryGetByOpCode((byte)(chipOpCode - 1), NfcDirection.Request);
            byte[] payload = fullData.Skip(2).ToArray();
            
            var packetResult = new NfcPacket
            {
                IsSuccess = true,
                Definition = definition,
                Payload = payload
            };

            if (definition != null)
            {
                packetResult.Descriptors.AddRange(_registry.Interpret(definition, payload.AsMemory()));
            }

            packet = packetResult;
        }

        return true;
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
        byte lcs = (byte)((~len + 1) & 0xFF);
        byte sum = 0;
        foreach (byte b in body) sum += b;
        byte dcs = (byte)((~sum + 1) & 0xFF);

        byte[] frame = new byte[len + 7];
        frame[0] = 0x00; frame[1] = 0x00; frame[2] = 0xFF;
        frame[3] = (byte)len; frame[4] = lcs;
        Array.Copy(body, 0, frame, 5, len);
        frame[len + 5] = dcs; frame[len + 6] = 0x00;
        return frame;
    }
}
