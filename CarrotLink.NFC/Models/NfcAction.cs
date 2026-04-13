namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 通信原子操作枚举
/// 区分物理层、芯片层和卡片层动作。
/// </summary>
public enum NfcAction
{
    // === 物理层 ===
    /// <summary>
    /// 物理绕过，直接发送原始通信帧
    /// </summary>
    Raw_Physical_Bypass,

    // === 卡片层 ===
    /// <summary>
    /// 卡片层透传
    /// </summary>
    Card_CommunicateThru,

    // 保留或其他的常用概念
    FieldOn,
    FieldOff,
    Halt,
    
    // 发起指令 (Request)
    REQA,
    WUPA,
    Anticoll,
    Select,
    ListPassiveTarget,
    Transceive,

    // 响应语义 (Result)
    GetAtqa,
    GetSak,
    GetUid,
    Response
}
