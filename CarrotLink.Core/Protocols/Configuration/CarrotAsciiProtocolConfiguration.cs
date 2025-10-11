using CarrotLink.Core.Devices.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Configuration
{
    public struct CarrotAsciiProtocolRegfileCommands
    {
        public string Name { get; set; }
        public string WriteRegCommand { get; set; }
        public string ReadRegCommand { get; set; }
        public string WriteBitsCommand { get; set; }
        public string ReadBitsCommand { get; set; }
    }

    /// <summary>
    /// 寄存器访问装饰器
    /// </summary>
    public enum CommandWrapper
    {
        /// <summary>
        /// 函数形式, reg_bits_write(HEX,DEC,DEC,HEX);
        /// </summary>
        Func = 0,
        /// <summary>
        /// 平铺参数, reg_bits_write;HEX;DEC;DEC;HEX;
        /// </summary>
        Flatten = 1
    }

    public class CarrotAsciiProtocolConfiguration : ProtocolConfigBase
    {
        public CommandWrapper CommandWrapper { get; set; } = CommandWrapper.Func;
        public CarrotAsciiProtocolRegfileCommands[] RegfilesCommands { get; set; } = Array.Empty<CarrotAsciiProtocolRegfileCommands>();
    }
}
