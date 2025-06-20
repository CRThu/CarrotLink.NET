using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    /// <summary>
    /// 协议基类
    /// </summary>
    public abstract class ProtocolBase : IProtocol
    {
        public abstract string ProtocolName { get; }
        public abstract int ProtocolVersion { get; }

        public abstract byte[] Encode(IPacket packet);

        public abstract bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet);
    }
}
