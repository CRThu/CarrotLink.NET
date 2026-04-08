using System;
using System.Collections.Generic;

namespace CarrotLink.NFC.Models;

/// <summary>
/// 指令方向。
/// </summary>
public enum NfcDirection
{
    /// <summary>
    /// 请求帧 (Host -> Card)
    /// </summary>
    Request,
    /// <summary>
    /// 响应帧 (Card -> Host)
    /// </summary>
    Response
}

/// <summary>
/// NFC 字段定义，用于描述数据布局。
/// </summary>
public record NfcFieldDefinition
{
    /// <summary>
    /// 字段名 (如 "Status", "Data")。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 字节长度。如果为 -1，则表示该字段为变长字段，将占据从当前位置到末尾的所有空间。
    /// </summary>
    public int Length { get; init; }

    /// <summary>
    /// 可选的字段说明信息。
    /// </summary>
    public string? Note { get; init; }
}

/// <summary>
/// NFC 帧定义模型。
/// 专注于卡片层（Layer 7）的指令描述，脱离特定的芯片传输协议。
/// </summary>
public record NfcFrameDefinition
{
    /// <summary>
    /// 助记符，用于逻辑识别 (如 "NTAG.READ")。
    /// </summary>
    public string Mnemonic { get; init; } = string.Empty;

    /// <summary>
    /// 物理层指令操作码（卡片层级，如 NTAG READ 为 0x30）。
    /// </summary>
    public byte OpCode { get; init; }

    /// <summary>
    /// 指令方向 (请求/响应)。
    /// </summary>
    public NfcDirection Direction { get; init; }

    /// <summary>
    /// 帧字段序列定义。
    /// </summary>
    public List<NfcFieldDefinition> Fields { get; init; } = new();

    /// <summary>
    /// 标记是否为芯片（如 PN532）系统级指令，系统指令不需要传输层套壳。
    /// </summary>
    public bool IsSystemCommand { get; init; }
}
