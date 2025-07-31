using CarrotLink.Core.Protocols.Configuration;
using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    public class ScpiProtocol : ProtocolBase
    {
        public override string ProtocolName => nameof(ScpiProtocol);

        public override int ProtocolVersion => 1;

        public ScpiProtocolConfiguration? config;

        public ScpiProtocol(ScpiProtocolConfiguration? _config)
        {
            config = _config;
            if (config != null)
            {
            }
        }

        public override byte[] Encode(IPacket packet)
        {
            return packet switch
            {
                ICommandPacket cmd => Encoding.ASCII.GetBytes(CommandPacket.AddLineEnding(cmd.Command)),
                _ => throw new NotSupportedException($"Unsupported packet type: {packet.PacketType}")
            };
        }

        public override bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
        {
            packet = default;
            var reader = new SequenceReader<byte>(buffer);

            if (!reader.TryReadTo(out ReadOnlySequence<byte> seq, (byte)'\n', true))
                return false;

            var payload = Encoding.ASCII.GetString(seq).TrimEnd('\r');

            packet = new CommandPacket(CommandPacket.AddLineEnding(payload));

            buffer = buffer.Slice(reader.Position);
            return true;
        }
    }
}
