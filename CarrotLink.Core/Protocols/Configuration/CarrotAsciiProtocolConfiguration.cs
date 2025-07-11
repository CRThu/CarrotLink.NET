using CarrotLink.Core.Devices.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Configuration
{
    public class CarrotAsciiProtocolConfiguration : ProtocolConfigBase
    {
        public int DataPacketLength { get; set; } = 256;
    }
}
