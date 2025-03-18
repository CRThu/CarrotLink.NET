using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices
{
    /// <summary>
    /// 串口校验位类型（兼容System.IO.Ports）
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SerialParity
    {
        None = Parity.None,
        Odd = Parity.Odd,
        Even = Parity.Even,
        Mark = Parity.Mark,
        Space = Parity.Space
    }

    /// <summary>
    /// 串口停止位类型
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SerialStopBits
    {
        One = StopBits.One,
        Two = StopBits.Two,
        OnePointFive = StopBits.OnePointFive
    }

    /// <summary>
    /// 串口设备配置（JSON示例：{"portName":"COM1","baudRate":115200,...}）
    /// </summary>
    public class SerialConfiguration : DeviceConfigurationBase
    {
        /// <summary>
        /// 串口号（必需）
        /// </summary>
        [JsonPropertyName("portName")]
        public required string PortName { get; init; }

        /// <summary>
        /// 波特率（默认115200）
        /// </summary>
        [JsonPropertyName("baudRate")]
        public int BaudRate { get; set; } = 115200;

        /// <summary>
        /// 数据位（默认8）
        /// </summary>
        [JsonPropertyName("dataBits")]
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 校验位（默认无）
        /// </summary>
        [JsonPropertyName("parity")]
        public SerialParity Parity { get; set; } = SerialParity.None;

        /// <summary>
        /// 停止位（默认1）
        /// </summary>
        [JsonPropertyName("stopBits")]
        public SerialStopBits StopBits { get; set; } = SerialStopBits.One;


        public override void Validate()
        {
            base.Validate();
        }
    }
}
