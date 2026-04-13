using CarrotLink.Core.Protocols.Impl;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Devices.Configuration;
using CarrotLink.NFC.Models;
using System.Buffers;
using CarrotLink.Core.Utility;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using CarrotLink.Core.Session;

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
    private NfcAction _lastAction = NfcAction.Raw_Physical_Bypass;
    private readonly object _contextLock = new();

    public Pn532HsuProtocol(NfcCommandRegistry registry)
    {
        _registry = registry;
    }

    public async Task InitializeAsync(DeviceSession session)
    {
        // 1. 唤醒并获取固件版本 - 内部构建 HSU 专属物理帧
        List<byte> wakeupSeq = new List<byte> { 0x55, 0x55, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        wakeupSeq.AddRange(BuildFrame(new byte[] { 0xD4, 0x02 })); // GetFirmwareVersion
        var wakeupReq = new NfcPacket { Action = NfcAction.Raw_Physical_Bypass, Direction = NfcDirection.Request, Payload = wakeupSeq.ToArray() };
        
        if (await session.ExchangeAsync<NfcPacket>(wakeupReq, null, 1000) is not { IsSuccess: true })
        {
            throw new DriverErrorException("握手超时或失败: Wakeup / GetFirmwareVersion");
        }

        // 2. 配置 (SAMConfiguration) - 内部构建物理帧
        var samPayload = BuildFrame(new byte[] { 0xD4, 0x14, 0x01, 0x14, 0x01 });
        var samReq = new NfcPacket { Action = NfcAction.Raw_Physical_Bypass, Direction = NfcDirection.Request, Payload = samPayload };
        
        if (await session.ExchangeAsync<NfcPacket>(samReq, null, 1000) is not { IsSuccess: true })
        {
            throw new DriverErrorException("握手超时或失败: SAMConfiguration");
        }
    }

    // PN532 ACK 帧: 00 00 FF 00 FF 00
    private static readonly byte[] PN532_ACK = { 0x00, 0x00, 0xFF, 0x00, 0xFF, 0x00 };

    public byte[] Encode(IPacket packet)
    {
        if (packet is not NfcPacket nfcPacket) return Array.Empty<byte>();

        // 上下文记忆：记录请求动作，以便解析响应
        lock (_contextLock)
        {
            _lastAction = nfcPacket.Action;
        }

        List<byte> body = new List<byte>();

        // 指令注释：依据 14443 抽象动作在底层自动打包符合 PN532 的物理帧
        switch (nfcPacket.Action)
        {
            case NfcAction.ListPassiveTarget:
                body.Add(0xD4); // TFI
                body.Add(0x4A); // OpCode InListPassiveTarget
                if (nfcPacket.Payload != null) body.AddRange(nfcPacket.Payload);
                return BuildFrame(body.ToArray());

            case NfcAction.GetAtqa:
            case NfcAction.GetSak:
            case NfcAction.GetUid:
                // 可以映射到相应的卡面请求或其他高级封装，为了保持示例简化暂时抛到底部
                goto default;

            case NfcAction.Card_CommunicateThru:
                // 卡片透传：自动追加 TFI + 0x42 (InCommunicateThru) + TargetID
                body.Add(0xD4);
                body.Add(0x42); 
                // 仅传递 0x42 等同于 0x40 透传，但对于纯不带奇偶校验或特定比特率的通信极为重要
                body.Add(0x01); // 默认 TargetID
                if (nfcPacket.Payload != null) body.AddRange(nfcPacket.Payload);
                return BuildFrame(body.ToArray());

            case NfcAction.Raw_Physical_Bypass:
                // 物理透传：直接放行 Payload，不加任何封装
                if (nfcPacket.Payload != null) body.AddRange(nfcPacket.Payload);
                return body.ToArray();

            default:
                // 默认进行基础加框 (可按需细化更多 14443 动词)
                if (nfcPacket.Payload != null) body.AddRange(nfcPacket.Payload);
                return BuildFrame(body.ToArray());
        }
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
            NfcAction contextAction;
            lock (_contextLock)
            {
                contextAction = _lastAction;
                // 注意：由于没有特定的 Definition 绑定了，我们可以不核销，保持最后的会话记录应对重试或迟到的响应
                // 极简实现下 _lastAction 是安全的
            }

            if (chipOpCode == 0x41 || chipOpCode == 0x43) // InDataExchange / InCommunicateThru Response
            {
                // 剥离芯片壳：D5 41/43 [Status] ...
                byte status = fullData.Length > 2 ? fullData[2] : (byte)0xFF;
                
                // 指令注释：提取纯净的卡片载荷 (跳过 D5 41/43 Status)
                var cardPayload = new ReadOnlyMemory<byte>(fullData).Slice(3);
                
                packet = new NfcPacket
                {
                    IsSuccess = status == 0,
                    Direction = NfcDirection.Response,
                    Action = contextAction,
                    Payload = cardPayload.ToArray()
                };
            }
            else
            {
                // 芯片系统指令响应 (D5 [OpCode+1] Payload)
                var payload = new ReadOnlyMemory<byte>(fullData).Slice(2);
                
                packet = new NfcPacket
                {
                    IsSuccess = true, // 简化：默认响应为真
                    Direction = NfcDirection.Response,
                    Action = contextAction,
                    Payload = payload.ToArray()
                };
            }

            return true;
        }
        catch
        {
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
