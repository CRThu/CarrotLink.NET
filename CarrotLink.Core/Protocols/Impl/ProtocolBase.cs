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
    public abstract class ProtocolBase : IProtocolParser
    {
        public static string Version { get; set; } = nameof(ProtocolBase);
        public string Name { get; set; }

        protected abstract bool TryDecode(ref ReadOnlySequence<byte> buffer, out PacketBase? packet);

        public bool TryParse(ref ReadOnlySequence<byte> buffer, out PacketBase? packet)
        {
            // 通用预处理（例如校验CRC）
            if (buffer.IsEmpty)
            {
                packet = null;
                return false;
            }

            return TryDecode(ref buffer, out packet);
        }
    }
}
