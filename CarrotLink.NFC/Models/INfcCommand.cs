namespace CarrotLink.NFC.Models;

/// <summary>
/// NFC 指令抽象接口。
/// </summary>
public interface INfcCommand
{
    /// <summary>
    /// 指令操作码 (OpCode)。
    /// </summary>
    byte OpCode { get; }

    /// <summary>
    /// 获取当前指令的所有字段描述符。
    /// </summary>
    /// <returns>字段描述符列表</returns>
    List<NfcFieldDescriptor> GetDescriptors();
}
