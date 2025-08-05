using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols.Configuration;
using CarrotLink.Core.Protocols.Impl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols
{
    public enum ProtocolType
    {
        CarrotAscii,
        CarrotBinary,
        Scpi
    }

    public static class ProtocolFactory
    {
        public static IProtocol Create(ProtocolType type, ProtocolConfigBase config)
        {
            return type switch
            {
                ProtocolType.CarrotAscii => new CarrotAsciiProtocol(config as CarrotAsciiProtocolConfiguration),
                ProtocolType.CarrotBinary => new CarrotBinaryProtocol(config as CarrotBinaryProtocolConfiguration),
                ProtocolType.Scpi => new ScpiProtocol(config as ScpiProtocolConfiguration),
                _ => throw new NotSupportedException($"Unsupported protocol type: {type}")
            };
        }
    }
}
