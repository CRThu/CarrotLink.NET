using CarrotLink.Core.Utility;

namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 字段描述符，用于定义数据段的语义与原始数值。
/// </summary>
/// <param name="FieldName">字段名 (如 "UID")</param>
/// <param name="Value">原始数据片段</param>
/// <param name="Description">描述信息 (如 "Target ID")</param>
public record NfcFieldDescriptor(string FieldName, ReadOnlyMemory<byte> Value, string? Description = null)
{
    /// <summary>
    /// 返回字段的十六进制字符串表示。
    /// </summary>
    public string HexValue => Value.Span.BytesToHexString();

    /// <summary>
    /// 返回完整的描述字符串，用于日志打印。
    /// </summary>
    public override string ToString() => $"{FieldName}:{HexValue}";
}
