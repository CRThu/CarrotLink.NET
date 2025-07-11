using CarrotLink.Core.Protocols.Configuration;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Utility;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CarrotLink.Core.Protocols.Impl
{
    public class CarrotAsciiProtocol : ProtocolBase
    {
        public override string ProtocolName => nameof(CarrotAsciiProtocol);
        public override int ProtocolVersion => 2;
        public CarrotAsciiProtocolConfiguration? config;

        private readonly CarrotBinaryProtocol _innerProtocol;

        public CarrotAsciiProtocol(CarrotAsciiProtocolConfiguration? _config)
        {
            config = _config;
            if (config != null)
            {
                _innerProtocol = new CarrotBinaryProtocol(new CarrotBinaryProtocolConfiguration()
                {
                    CommandPacketLength = 256,
                    DataPacketLength = config.DataPacketLength
                });
            }
        }

        public override byte[] Encode(IPacket packet)
        {
            return packet switch
            {
                ICommandPacket cmd => Encoding.ASCII.GetBytes(cmd.Command),
                _ => throw new NotSupportedException($"Unsupported packet type: {packet.PacketType}")
            };
        }

        public override bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
        {
            packet = default;
            var reader = new SequenceReader<byte>(buffer);

            // 检查嵌套协议头
            if (!reader.TryPeek(out var startByte))
                return false;

            // 解码嵌套协议
            if (startByte == CarrotBinaryProtocol.StartByte)
                return _innerProtocol.TryDecode(ref buffer, out packet);

            // 检查ascii协议完整包
            if (!reader.TryReadTo(out ReadOnlySequence<byte> commandSeq, (byte)'\n', true))
                return false;

            // 解码ascii协议
            var cmd = Encoding.ASCII.GetString(commandSeq).TrimEnd('\r');
            packet = new CommandPacket(cmd);
            buffer = buffer.Slice(reader.Position);
            return true;
        }
    }
}
