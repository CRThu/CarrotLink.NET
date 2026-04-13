using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;

namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 结构化报文模型，支持动态自解释展示。
/// 作为极简数据模型，仅保留方向、语义动作、逻辑参数，彻底剥离业务解析逻辑。
/// </summary>
public record NfcPacket : ICommandPacket
{
    public PacketType PacketType => PacketType.Command;

    /// <summary>
    /// 指令方向 (请求/响应)。
    /// </summary>
    public NfcDirection Direction { get; init; }

    /// <summary>
    /// 语义化动作枚举。
    /// </summary>
    public NfcAction Action { get; init; }

    /// <summary>
    /// 指令注释（满足 ICommandPacket 接口，可直接映射为可视化的形式或空）
    /// </summary>
    public string Command => ToString();

    /// <summary>
    /// 报文纯逻辑载荷（不包含物理帧头和 OpCode 的纯参数）。
    /// </summary>
    public byte[]? Payload { get; init; }

    /// <summary>
    /// 业务层逻辑成功标志。
    /// </summary>
    public bool IsSuccess { get; init; }

    public override string ToString()
    {
        var hex = Payload != null ? Payload.BytesToHexString() : string.Empty;
        return $"[Raw Hex: {hex}] {Direction} {Action}".Trim();
    }
}
