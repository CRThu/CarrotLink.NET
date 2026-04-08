using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 结构化报文模型，支持动态自解释展示。
/// </summary>
public record NfcPacket : ICommandPacket
{
    public PacketType PacketType => PacketType.Command;

    /// <summary>
    /// 自解释指令描述。
    /// 逻辑：根据 Definition 切分 Payload 展示字段，否则退化为 HEX 模式。
    /// </summary>
    public string Command
    {
        get
        {
            var mnemonic = Definition?.Mnemonic ?? (string.IsNullOrEmpty(Mnemonic) ? "Unknown" : Mnemonic);
            
            if (Definition == null || Payload == null || Payload.Length == 0)
            {
                var hex = Payload != null ? Payload.BytesToHexString() : string.Empty;
                return $"[{mnemonic}] {hex}".Trim();
            }

            // 根据 Definition 切片展示
            StringBuilder sb = new StringBuilder();
            sb.Append($"[{mnemonic}] {{");
            
            try
            {
                int offset = 0;
                var byteSpan = Payload.AsSpan();
                
                for (int i = 0; i < Definition.Fields.Count; i++)
                {
                    var field = Definition.Fields[i];
                    if (offset >= byteSpan.Length) break;

                    int len = field.Length == -1 ? byteSpan.Length - offset : field.Length;
                    if (offset + len > byteSpan.Length) len = byteSpan.Length - offset;

                    var segment = byteSpan.Slice(offset, len);
                    sb.Append($"{field.Name}: {segment.ToArray().BytesToHexString()}");
                    
                    if (i < Definition.Fields.Count - 1 && offset + len < byteSpan.Length)
                        sb.Append(", ");

                    offset += len;
                }

                // 处理剩余未知数据
                if (offset < byteSpan.Length)
                {
                    if (sb.Length > mnemonic.Length + 4) sb.Append(", ");
                    sb.Append($"Unknown: {byteSpan.Slice(offset).ToArray().BytesToHexString()}");
                }
            }
            catch
            {
                sb.Append($"Error: {Payload.BytesToHexString()}");
            }

            sb.Append("}");
            return sb.ToString();
        }
    }

    /// <summary>
    /// 语义化动作枚举（保留兼容）。
    /// </summary>
    public NfcAction Action { get; init; }

    /// <summary>
    /// 助记符。
    /// </summary>
    public string Mnemonic { get; init; } = string.Empty;

    /// <summary>
    /// 当前报文绑定的动态定义。
    /// </summary>
    public NfcFrameDefinition? Definition { get; init; }

    /// <summary>
    /// 报文原始载荷。
    /// </summary>
    public byte[]? Payload { get; init; }

    /// <summary>
    /// 字段描述符列表（用于逻辑访问，可由定义生成）。
    /// </summary>
    public List<NfcFieldDescriptor> Descriptors { get; init; } = new();

    /// <summary>
    /// 业务层逻辑成功标志。
    /// </summary>
    public bool IsSuccess { get; init; }

    public override string ToString() => Command;
}
