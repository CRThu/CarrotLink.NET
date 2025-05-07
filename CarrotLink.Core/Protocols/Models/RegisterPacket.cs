using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public record RegisterPacket : IPacket
    {
        private readonly PacketType _type;
        private readonly (int _oper, int _regfile, int _addr, int _value) _data;

        public PacketType Type => _type;
        public (int _oper, int _regfile, int _addr, int _value) Payload => _data;

        public RegisterPacket(int oper, int regfile, int addr, int value, PacketType type = PacketType.Command)
        {
            _data._oper = oper;
            _data._regfile = regfile;
            _data._addr = addr;
            _data._value = value;
            _type = type;
        }

        public byte[] Pack(IProtocol protocol)
            => protocol.Pack(this);

        public override string ToString()
            => $"{Payload._oper}, {Payload._regfile}, {Payload._addr}, {Payload._value}";
    }
}
