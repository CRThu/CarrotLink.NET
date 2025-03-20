using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    public interface IProtocolParser
    {
        public string Name { get; set; }
        public static string Version { get; }
        public bool TryParse(ref ReadOnlySequence<byte> buffer, out PacketBase? packet);
    }
}
