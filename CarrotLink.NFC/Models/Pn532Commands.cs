using CarrotLink.Core.Utility;

namespace CarrotLink.NFC.Models;

/// <summary>
/// PN532 InListPassiveTarget (0x4A) 指令参数。
/// </summary>
public class ListPassiveTargetParams : INfcCommand
{
    public byte OpCode => 0x4A;
    public byte MaxTargets { get; set; } = 0x01;
    public byte Brty { get; set; } = 0x00; // 106 kbps type A (ISO/IEC 14443 Type A)

    public List<NfcFieldDescriptor> GetDescriptors()
    {
        return new List<NfcFieldDescriptor>
        {
            new NfcFieldDescriptor("MaxTargets", new byte[] { MaxTargets }, "Maximum number of targets"),
            new NfcFieldDescriptor("Brty", new byte[] { Brty }, "Baud rate and type")
        };
    }
}

/// <summary>
/// PN532 InDataExchange (0x40) - Mifare Authenticate 子指令。
/// </summary>
public class MifareAuthParams : INfcCommand
{
    public byte OpCode => 0x40; // InDataExchange
    public byte Tg { get; set; } = 0x01;
    public byte AuthCommand { get; set; } // 0x60 (KeyA) or 0x61 (KeyB)
    public byte BlockAddr { get; set; }
    public byte[] Key { get; set; } = new byte[6];
    public byte[] Uid { get; set; } = new byte[4];

    public List<NfcFieldDescriptor> GetDescriptors()
    {
        var data = new List<byte> { AuthCommand, BlockAddr };
        data.AddRange(Key);
        data.AddRange(Uid);

        return new List<NfcFieldDescriptor>
        {
            new NfcFieldDescriptor("Tg", new byte[] { Tg }, "Target number"),
            new NfcFieldDescriptor("Data", data.ToArray(), "Auth payload")
        };
    }
}

/// <summary>
/// PN532 InCommunicateThru (0x42) 透传指令。
/// </summary>
public class CommunicateThruParams : INfcCommand
{
    public byte OpCode => 0x42;
    public byte[] Data { get; set; } = Array.Empty<byte>();

    public List<NfcFieldDescriptor> GetDescriptors()
    {
        return new List<NfcFieldDescriptor>
        {
            new NfcFieldDescriptor("Data", Data, "Raw data to transmit")
        };
    }
}
