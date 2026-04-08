using CarrotLink.Core.Session;
using CarrotLink.Core.Utility;
using CarrotLink.NFC.Models;
using CarrotLink.NFC.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarrotLink.NFC.Session;

/// <summary>
/// NFC 会话扩展方法。
/// </summary>
public static class NfcSessionExtensions
{
    private static NfcCommandRegistry GetRegistry(DeviceSession session)
    {
        if (session.Protocol is Pn532HsuProtocol pn532Protocol)
        {
            return pn532Protocol.Registry;
        }
        throw new InvalidOperationException("当前协议不支持 NFC 动态映射。");
    }

    /// <summary>
    /// 发送 NFC 指令字符串。
    /// 示例："NTAG.READ 04" -> 发送读取 04 页指令。
    /// 示例："HEX 30 04" -> 直接发送原始字节 30 04。
    /// </summary>
    public static async Task SendNfcAsync(this DeviceSession session, string cmdLine)
    {
        if (string.IsNullOrWhiteSpace(cmdLine)) return;

        // 1. HEX 逃逸逻辑
        if (cmdLine.StartsWith("HEX ", StringComparison.OrdinalIgnoreCase))
        {
            var rawHex = cmdLine.Substring(4);
            var payload = rawHex.HexStringToBytes();
            await session.WriteAsync(new NfcPacket { Payload = payload });
            return;
        }

        // 2. 助记符解析
        var parts = cmdLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mnemonic = parts[0];
        
        var registry = GetRegistry(session);
        var definition = registry.TryGetByMnemonic(mnemonic);

        if (definition == null)
        {
            throw new KeyNotFoundException($"[Registry] 未能识别的助记符: {mnemonic}");
        }

        // 3. 将后续参数合并为载荷
        var paramBytes = new List<byte>();
        for (int i = 1; i < parts.Length; i++)
        {
            paramBytes.AddRange(parts[i].HexStringToBytes());
        }

        await session.WriteAsync(new NfcPacket
        {
            Definition = definition,
            Payload = paramBytes.ToArray()
        });
    }
}
