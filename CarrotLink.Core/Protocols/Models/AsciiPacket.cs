using CarrotLink.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public record AsciiPacket : IPacket
    {
        private readonly PacketType _type;
        private readonly string _msg;

        public PacketType Type => _type;
        public string Payload => _msg;

        public AsciiPacket(string Message, PacketType type = PacketType.Command)
        {
            _msg = Message;
            _type = type;
        }

        public override string ToString()
            => $"{Payload.ReplaceLineEndings("\\r\\n")}";

    }
}
