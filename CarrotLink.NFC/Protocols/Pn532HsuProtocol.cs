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
    private NfcFrameDefinition? _lastRequestDefinition;
    private readonly object _contextLock = new();

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
            // 上下文记忆：记录请求定义，以便解析响应
            lock (_contextLock)
            {
                _lastRequestDefinition = def;
            }

            if (def.IsSystemCommand || def.Mnemonic.StartsWith("PN532.", StringComparison.OrdinalIgnoreCase))
            {
                // 系统指令：直接封装 OpCode
                body.Add(def.OpCode);
            }
            else
            {
                // 卡片事务：自动套壳 InDataExchange (0x40) + Tg (0x01)
                body.Add(0x40);
                body.Add(0x01);
                body.Add(def.OpCode);
            }
        }
        else if (nfcPacket.Payload != null)
        {
            // HEX 逃逸逻辑：直接发送原始载荷
            body.Clear();
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

        try
        {
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

            // 4. 链路层适配与上下文解析
            NfcFrameDefinition? contextDef;
            lock (_contextLock)
            {
                contextDef = _lastRequestDefinition;
                _lastRequestDefinition = null; // 触发解析后立即核销
            }

            if (chipOpCode == 0x41) // InDataExchange Response
            {
                // 剥离芯片壳：D5 41 [Status] ...
                byte status = fullData.Length > 2 ? fullData[2] : (byte)0xFF;
                
                // 指令注释：提取纯净的卡片载荷 (跳过 D5 41 Status)
                var cardPayload = new ReadOnlyMemory<byte>(fullData).Slice(3);
                
                var packetResult = new NfcPacket
                {
                    IsSuccess = status == 0,
                    Definition = contextDef,
                    Direction = NfcDirection.Response,
                    Payload = cardPayload.ToArray()
                };

                // 使用上下文定义进行自解释解析
                if (contextDef != null)
                {
                    packetResult.Descriptors.AddRange(_registry.Interpret(contextDef, NfcDirection.Response, cardPayload));
                }

                packet = packetResult;
            }
            else
            {
                // 芯片系统指令响应 (D5 [OpCode+1] Payload)
                var payload = new ReadOnlyMemory<byte>(fullData).Slice(2);
                
                var packetResult = new NfcPacket
                {
                    IsSuccess = true,
                    Definition = contextDef,
                    Direction = NfcDirection.Response,
                    Payload = payload.ToArray()
                };

                if (contextDef != null)
                {
                    packetResult.Descriptors.AddRange(_registry.Interpret(contextDef, NfcDirection.Response, payload));
                }

                packet = packetResult;
            }

            return true;
        }
        catch
        {
            // 状态清理：异常时强制清理上下文
            lock (_contextLock)
            {
                _lastRequestDefinition = null;
            }
            throw;
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
