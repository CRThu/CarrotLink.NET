using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public interface IProtocol
    {
        public static string Name { get; set; }
        public static string Version { get; }

        public byte[] GetBytes(IPacket packet);
        public bool TryParse(ref ReadOnlySequence<byte> buffer, out IPacket? packet);
    }
}
