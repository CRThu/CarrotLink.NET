using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public record RegisterPacket(int Oper, int RegFile, int Addr, int Value) : IPacket
    {
        public PacketType Type => PacketType.Register;
        public (int, int, int, int) Payload => (Oper, RegFile, Addr, Value);
        public byte[] Pack(IProtocol protocol) => protocol.Pack(this);
        public override string ToString()
        {
            return $"{Payload.Item1},{Payload.Item2},{Payload.Item3},{Payload.Item4}";
        }
    }
}
