using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Protocols.Configuration;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    public class ScpiProtocol : IProtocol
    {
        public string ProtocolName => nameof(ScpiProtocol);

        public int ProtocolVersion => 1;

        public ProtocolConfigBase Config => _config;

        private ScpiProtocolConfiguration _config;

        public ScpiProtocol(ScpiProtocolConfiguration? config)
        {
            _config = config;
            if (_config != null)
            {
            }
        }

        public byte[] Encode(IPacket packet)
        {
            return packet switch
            {
                ICommandPacket cmd => Encoding.ASCII.GetBytes(CommandPacket.AddLineEnding(cmd.Command)),
                _ => throw new NotSupportedException($"Unsupported packet type: {packet.PacketType}")
            };
        }

        public bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
        {
            packet = default;
            var reader = new SequenceReader<byte>(buffer);

            if (!reader.TryReadTo(out ReadOnlySequence<byte> seq, (byte)'\n', true))
                return false;

            var payload = Encoding.ASCII.GetString(seq).TrimEnd('\r');

            if (StringEx.TryToDouble(payload, out var doubleValue))
            {
                packet = new DataPacket(new double[] { doubleValue });
            }
            else
            {
                packet = new CommandPacket(CommandPacket.AddLineEnding(payload));
            }

            buffer = buffer.Slice(reader.Position);
            return true;
        }
    }
}
