using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public interface IPacket
    {
        PacketType Type { get; }
    }

    public enum PacketType { Command, Data }

}
