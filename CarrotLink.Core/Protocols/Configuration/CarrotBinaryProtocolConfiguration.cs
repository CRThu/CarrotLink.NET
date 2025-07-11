using CarrotLink.Core.Devices.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Configuration
{
    public class CarrotBinaryProtocolConfiguration : ProtocolConfigBase
    {
        public int CommandPacketLength { get; set; } = 256;
        public int DataPacketLength { get; set; } = 256;
    }
}
