using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace CarrotLink.Core.Protocols.Models
{
    public enum PacketType { Command, Data, Register }

    public interface IPacket
    {
        public PacketType PacketType { get; }
    }
}
