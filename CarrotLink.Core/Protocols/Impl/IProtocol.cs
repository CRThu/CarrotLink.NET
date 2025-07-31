using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    public interface IProtocol
    {
        public string ProtocolName { get; }
        public int ProtocolVersion { get; }
        public ProtocolConfigBase Config { get; }

        public byte[] Encode(IPacket packet);
        public bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet);
    }
}
