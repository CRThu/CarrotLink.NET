using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;

namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 结构化报文模型，基于助记符与字段描述符。
/// </summary>
public record NfcPacket : ICommandPacket
{
    /// <summary>
    /// 实现 ICommandPacket 接口，固定为 Command 类型。
    /// </summary>
    public PacketType PacketType => PacketType.Command;

    /// <summary>
    /// 计算属性：聚合展示助记符与格式化后的字段列表。
    /// </summary>
    public string Command
    {
        get
        {
            var fields = IsSuccess ? ResponseFields : RequestFields;
            var fieldsStr = fields != null && fields.Count > 0 
                ? $" {{{string.Join(", ", fields)}}}" 
                : "";
            return $"[{Mnemonic}] ({Action}, OK={IsSuccess}){fieldsStr}";
        }
    }

    /// <summary>
    /// 语义化动作枚举。
    /// </summary>
    public NfcAction Action { get; init; }

    /// <summary>
    /// 助记符 (如 "PN532.ListTarget")。
    /// </summary>
    public string Mnemonic { get; init; } = string.Empty;

    /// <summary>
    /// 请求字段列表。
    /// </summary>
    public List<NfcFieldDescriptor> RequestFields { get; init; } = new();

    /// <summary>
    /// 响应字段列表。
    /// </summary>
    public List<NfcFieldDescriptor> ResponseFields { get; init; } = new();

    /// <summary>
    /// 原始指令定义 (用于 Encode)。
    /// </summary>
    public INfcCommand? CommandDefinition { get; init; }

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
