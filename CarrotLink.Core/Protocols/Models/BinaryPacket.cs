using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public record BinaryPacket : IPacket
    {
        private readonly PacketType _type;
        private readonly byte[] _data;

        public PacketType Type => _type;
        public byte[] Payload => _data;


        public BinaryPacket(byte[] data, PacketType type = PacketType.Data)
        {
            _data = data;
            _type = type;
        }

        public byte[] Pack(IProtocol protocol)
            => protocol.Pack(this);

        public override string ToString()
            => $"<{(ushort)(Payload[6] << 8 | Payload[5])} Bytes Data>";
    }

}
