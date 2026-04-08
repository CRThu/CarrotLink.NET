using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarrotLink.NFC.Utility;

/// <summary>
/// 简单字节的十六进制字符串 JSON 转换器 (支持 "0x" 前缀)。
/// </summary>
public class ByteHexConverter : JsonConverter<byte>
{
    public override byte Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (value != null && value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToByte(value.Substring(2), 16);
        }
        return byte.Parse(value ?? "0");
    }

    public override void Write(Utf8JsonWriter writer, byte value, JsonSerializerOptions options) 
        => writer.WriteStringValue($"0x{value:X2}");
}

/// <summary>
/// 可空字节的十六进制字符串 JSON 转换器 (支持 "0x" 前缀)。
/// </summary>
public class NullableByteHexConverter : JsonConverter<byte?>
{
    public override byte? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return null;
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return Convert.ToByte(value.Substring(2), 16);
        }
        return byte.Parse(value);
    }

    public override void Write(Utf8JsonWriter writer, byte? value, JsonSerializerOptions options)
    {
        if (value == null) writer.WriteNullValue();
        else writer.WriteStringValue($"0x{value.Value:X2}");
    }
}
