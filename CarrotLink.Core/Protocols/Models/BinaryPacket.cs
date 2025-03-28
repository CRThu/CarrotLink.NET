using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public record BinaryPacket(byte[] Data) : IPacket
    {
        public PacketType Type => PacketType.Binary;
        public byte[] Payload => Data;
        public byte[] Pack(IProtocol protocol) => protocol.Pack(this);
        public override string ToString()
        {
            return $"<{(ushort)(Payload[6] << 8 | Payload[5])} Bytes Data>";
        }
    }

}
