using CarrotLink.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public record AsciiPacket(string Message) : IPacket
    {
        public PacketType Type => PacketType.Ascii;
        public string Payload => Message;
        public byte[] Pack(IProtocol protocol) => protocol.Pack(this);

        public override string ToString()
        {
            return Payload.TrimEnd("\r\n".ToCharArray());
        }
    }
}
