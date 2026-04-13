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
            await session.WriteAsync(new NfcPacket { Action = NfcAction.Raw_Physical_Bypass, Direction = NfcDirection.Request, Payload = payload });
            return;
        }

        // 2. 解析助记符与参数
        var parts = cmdLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var mnemonic = parts[0];
        var hexParams = parts.Skip(1).ToArray();

        await session.SendNfcAsync(mnemonic, hexParams);
    }

    /// <summary>
    /// 发送带参数的 NFC 指令。
    /// 根据注册表定义的 RequestFields 自动按位填充。
    /// </summary>
    public static async Task SendNfcAsync(this DeviceSession session, string mnemonic, params string[] hexParams)
    {
        var registry = GetRegistry(session);
        var definition = registry.TryGetByMnemonic(mnemonic);

        if (definition == null)
        {
            throw new KeyNotFoundException($"[Registry] 未能识别的助记符: {mnemonic}");
        }

        var paramBytes = new List<byte>();
        var fields = definition.RequestFields;

        // 自动化参数序列化：根据定义填充
        for (int i = 0; i < fields.Count; i++)
        {
            if (i >= hexParams.Length) break;

            var field = fields[i];
            var bytes = hexParams[i].HexStringToBytes();

            if (field.Length == -1)
            {
                // 变长字段：直接填充剩余所有输入的十六进制参数
                paramBytes.AddRange(bytes);
                for (int j = i + 1; j < hexParams.Length; j++)
                {
                    paramBytes.AddRange(hexParams[j].HexStringToBytes());
                }
                break;
            }

            // 定长字段：截断或填充
            if (bytes.Length > field.Length)
            {
                paramBytes.AddRange(bytes.Take(field.Length));
            }
            else
            {
                paramBytes.AddRange(bytes);
                // 如果长度不足，保持原样（由底层协议或卡片处理）
            }
        }

        // 如果没有字段定义但有参数，则直接按顺序追加 (兜底逻辑)
        if (fields.Count == 0 && hexParams.Length > 0)
        {
            foreach (var p in hexParams) paramBytes.AddRange(p.HexStringToBytes());
        }

        // 默认作为卡片透传，如果 registry 定义了具体的高级 Action 则可以进一步根据 json 的扩展解析
        var action = NfcAction.Card_CommunicateThru;

        await session.WriteAsync(new NfcPacket
        {
            Action = action,
            Direction = NfcDirection.Request,
            Payload = paramBytes.ToArray()
        });
    }
}
