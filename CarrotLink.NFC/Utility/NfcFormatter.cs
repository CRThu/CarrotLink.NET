using CarrotLink.NFC.Models;
using CarrotLink.NFC.Protocols;
using CarrotLink.Core.Utility;
using System;
using System.Text;

namespace CarrotLink.NFC.Utility;

/// <summary>
/// 提供 NFC 报文的格式化展示服务，剥离原本在 NfcPacket 中的负责展示的方法。
/// </summary>
public static class NfcFormatter
{
    /// <summary>
    /// 将基础结构的 NfcPacket 映射到具体的业务字典中，并对其载荷进行可视化解释。
    /// </summary>
    /// <param name="packet">待解析的包</param>
    /// <param name="registry">用来查询指令和切分的注册表</param>
    /// <param name="mnemonicHint">可选的助记符暗示，如果包有特定语义</param>
    /// <returns>展示友好的可读报文</returns>
    public static string Format(NfcPacket packet, NfcCommandRegistry registry, string? mnemonicHint = null)
    {
        var hex = packet.Payload != null ? packet.Payload.BytesToHexString() : string.Empty;

        // 若没有业务载荷，则直接返回基础原始 hex
        if (packet.Payload == null || packet.Payload.Length == 0)
        {
            return $"[Raw Hex: {hex}] {packet.Direction} {packet.Action} {mnemonicHint}".Trim();
        }

        NfcFrameDefinition? def = null;
        if (!string.IsNullOrEmpty(mnemonicHint))
        {
            def = registry.TryGetByMnemonic(mnemonicHint);
        }

        if (def == null)
        {
            return $"[Raw Hex: {hex}] {packet.Direction} {packet.Action} {mnemonicHint}".Trim();
        }

        string mnemonic = def.Mnemonic;

        // 根据方向选择字段定义
        var fields = packet.Direction == NfcDirection.Request ? def.RequestFields : def.ResponseFields;
        if (fields == null || fields.Count == 0)
        {
            return $"[Raw Hex: {hex}] [{mnemonic}] {packet.Direction}";
        }

        // 根据 Definition 切片展示
        StringBuilder sb = new StringBuilder();
        sb.Append($"[Raw Hex: {hex}] [{mnemonic}] {packet.Direction} {{");

        try
        {
            int offset = 0;
            var byteSpan = packet.Payload.AsSpan();

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (offset >= byteSpan.Length) break;

                // 变长字段处理
                int len = field.Length == -1 ? byteSpan.Length - offset : field.Length;
                if (offset + len > byteSpan.Length) len = byteSpan.Length - offset;

                var segment = byteSpan.Slice(offset, len);
                sb.Append($"{field.Name}: {segment.ToArray().BytesToHexString()}");

                if (i < fields.Count - 1 && offset + len < byteSpan.Length)
                    sb.Append(", ");

                offset += len;
            }

            // 余量未知数据展示
            if (offset < byteSpan.Length)
            {
                if (sb.Length > mnemonic.Length + 4) sb.Append(", ");
                sb.Append($"Unknown: {byteSpan.Slice(offset).ToArray().BytesToHexString()}");
            }
        }
        catch
        {
            sb.Append($"Error: {hex}");
        }

        sb.Append("}");
        return sb.ToString();
    }
}
