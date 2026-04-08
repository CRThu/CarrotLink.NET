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
    
    // Key: (Direction << 8) | OpCode. 确保同一操作码在请求/响应方向上不冲突
    private readonly Dictionary<int, NfcFrameDefinition> _opCodeMap = new();

    public NfcCommandRegistry()
    {
        InitializeSystemCommands();
    }

    /// <summary>
    /// 初始化 PN532 等芯片的系统级指令。
    /// 系统指令通常是直接物理对等的。
    /// </summary>
    private void InitializeSystemCommands()
    {
        RegisterInternal(new NfcFrameDefinition
        {
            Mnemonic = "PN532.SAMConfig",
            OpCode = 0x14,
            IsSystemCommand = true,
            Direction = NfcDirection.Request,
            Fields = new List<NfcFieldDefinition>
            {
                new() { Name = "Mode", Length = 1 },
                new() { Name = "Timeout", Length = 1 },
                new() { Name = "IRQ", Length = 1 }
            }
        });

        RegisterInternal(new NfcFrameDefinition
        {
            Mnemonic = "PN532.GetVersion",
            OpCode = 0x02,
            IsSystemCommand = true,
            Direction = NfcDirection.Request
        });

        // 更多系统指令...
    }

    /// <summary>
    /// 从 `.dev/nfc/` 目录加载 JSON 定义。
    /// </summary>
    public void LoadFromDirectory(string directoryPath = ".dev/nfc/")
    {
        if (!Directory.Exists(directoryPath)) return;

        var files = Directory.GetFiles(directoryPath, "*.json");
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
                            RegisterInternal(definition);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 标准注释：捕获 JSON 格式错误并打印详细信息，防止程序因配置损坏而崩溃。
                Console.WriteLine($"[NfcRegistry] 配置文件加载失败: {file}, Error: {ex.Message}");
            }
        }
    }

    private void RegisterInternal(NfcFrameDefinition definition)
    {
        _mnemonicMap[definition.Mnemonic] = definition;
        
        //标准注释：使用 Direction 和 OpCode 的组合键，解决同一 OpCode 在双向通信中的语义冲突。
        int key = ((int)definition.Direction << 8) | definition.OpCode;
        _opCodeMap[key] = definition;
    }

    public NfcFrameDefinition? TryGetByMnemonic(string mnemonic)
    {
        return _mnemonicMap.TryGetValue(mnemonic, out var def) ? def : null;
    }

    public NfcFrameDefinition? TryGetByOpCode(byte opCode, NfcDirection direction)
    {
        int key = ((int)direction << 8) | opCode;
        return _opCodeMap.TryGetValue(key, out var def) ? def : null;
    }

    /// <summary>
    /// 根据定义将原始载荷解析为描述符列表。
    /// </summary>
    public List<NfcFieldDescriptor> Interpret(NfcFrameDefinition definition, ReadOnlyMemory<byte> payload)
    {
        var descriptors = new List<NfcFieldDescriptor>();
        var current = payload;

        foreach (var field in definition.Fields)
        {
            if (current.IsEmpty) break;

            int len = field.Length;
            if (len == -1) // 变长逻辑
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
