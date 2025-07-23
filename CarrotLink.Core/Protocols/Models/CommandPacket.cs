using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public interface ICommandPacket : IPacket
    {
        public string Command { get; }
    }

    public record CommandPacket(string Command) : ICommandPacket
    {
        public PacketType PacketType => PacketType.Command;

        public override string ToString() => Command;

        public static string AddLineEnding(string cmd)
        {
            return cmd.EndsWith('\n') ? cmd : cmd + "\n";
        }
    }
}
