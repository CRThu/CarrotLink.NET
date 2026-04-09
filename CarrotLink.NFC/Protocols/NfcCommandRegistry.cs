using CarrotLink.NFC.Models;
using CarrotLink.NFC.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CarrotLink.NFC.Protocols;

/// <summary>
/// 通用指令注册表，实现卡片指令定义的动态管理。
/// </summary>
public class NfcCommandRegistry
{
    private readonly Dictionary<string, NfcFrameDefinition> _mnemonicMap = new();

    public NfcCommandRegistry()
    {
    }

    /// <summary>
    /// 从 `.dev/nfc/` 目录递归加载 JSON 定义。
    /// </summary>
    public void LoadFromDirectory(string directoryPath = ".dev/nfc/")
    {
        if (!Directory.Exists(directoryPath)) return;

        // 支持子目录递归扫描
        var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.AllDirectories);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new ByteHexConverter(), new NullableByteHexConverter() }
        };

        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var container = JsonSerializer.Deserialize<CardDefContainer>(json, options);
                if (container?.Cards != null)
                {
                    foreach (var cardType in container.Cards)
                    {
                        var prefix = cardType.Key;
                        foreach (var frame in cardType.Value)
                        {
                            var fullMnemonic = string.IsNullOrEmpty(frame.Mnemonic) ? prefix : $"{prefix}.{frame.Mnemonic}";
                            var definition = frame with { Mnemonic = fullMnemonic };
                            // 后加载的定义覆盖前者（便于补丁更新）
                            _mnemonicMap[definition.Mnemonic] = definition;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NfcRegistry] 配置文件加载失败: {file}, Error: {ex.Message}");
            }
        }
    }

    public NfcFrameDefinition? TryGetByMnemonic(string mnemonic)
    {
        return _mnemonicMap.TryGetValue(mnemonic, out var def) ? def : null;
    }

    /// <summary>
    /// 根据方向和定义将原始载荷解析为描述符列表。
    /// </summary>
    public List<NfcFieldDescriptor> Interpret(NfcFrameDefinition definition, NfcDirection direction, ReadOnlyMemory<byte> payload)
    {
        var descriptors = new List<NfcFieldDescriptor>();
        var current = payload;
        var fields = direction == NfcDirection.Request ? definition.RequestFields : definition.ResponseFields;

        if (fields == null) return descriptors;

        foreach (var field in fields)
        {
            if (current.IsEmpty) break;

            int len = field.Length;
            if (len == -1) // 变长字段处理：消耗剩余载荷
            {
                descriptors.Add(new NfcFieldDescriptor(field.Name, current, field.Note));
                current = ReadOnlyMemory<byte>.Empty;
                break;
            }

            if (current.Length >= len)
            {
                descriptors.Add(new NfcFieldDescriptor(field.Name, current.Slice(0, len), field.Note));
                current = current.Slice(len);
            }
            else
            {
                descriptors.Add(new NfcFieldDescriptor(field.Name, current, field.Note));
                current = ReadOnlyMemory<byte>.Empty;
                break;
            }
        }

        if (!current.IsEmpty)
        {
            descriptors.Add(new NfcFieldDescriptor("Unknown", current));
        }

        return descriptors;
    }

    /// <summary>
    /// 卡片定义容器。
    /// </summary>
    private class CardDefContainer
    {
        public Dictionary<string, List<NfcFrameDefinition>>? Cards { get; set; }
    }
}
