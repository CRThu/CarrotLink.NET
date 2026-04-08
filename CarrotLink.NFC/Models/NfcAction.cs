namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 通信原子操作枚举
/// </summary>
public enum NfcAction
{
    // 控制原语
    //None,
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
