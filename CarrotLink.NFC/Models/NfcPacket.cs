using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;

namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 结构化报文模型，基于助记符与语义化动作。
/// </summary>
public record NfcPacket : ICommandPacket
{
    /// <summary>
    /// 实现 ICommandPacket 接口，固定为 Command 类型。
    /// </summary>
    public PacketType PacketType => PacketType.Command;

    /// <summary>
    /// 计算属性：聚合展示助记符、数据载荷与动作状态。
    /// </summary>
    public string Command => $"{Mnemonic} [{(Payload != null ? Payload.BytesToHexString() : "<empty>")}] ({Action}, OK={IsSuccess})";

    /// <summary>
    /// 语义化动作枚举。
    /// </summary>
    public NfcAction Action { get; init; }

    /// <summary>
    /// 助记符 (如 "NTAG2.AUTH")。
    /// </summary>
    public string Mnemonic { get; init; } = string.Empty;

    /// <summary>
    /// 数据载荷。
    /// </summary>
    public byte[]? Payload { get; set; }

    /// <summary>
    /// 业务层逻辑成功标志。
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// 重写以支持日志直接输出。
    /// </summary>
    /// <returns>计算后的指令描述字符串</returns>
    public override string ToString() => Command;
}
