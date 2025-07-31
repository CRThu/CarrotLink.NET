using CarrotLink.Core.Devices.Configuration;
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
    public class CarrotAsciiProtocol : IProtocol
    {
        public string ProtocolName => nameof(CarrotAsciiProtocol);
        public int ProtocolVersion => 2;

        private readonly CarrotBinaryProtocol _innerProtocol;

        public ProtocolConfigBase Config => _config;

        private CarrotAsciiProtocolConfiguration _config;

        public CarrotAsciiProtocol(CarrotAsciiProtocolConfiguration? config)
        {
            _config = config;
            if (_config != null)
            {
                _innerProtocol = new CarrotBinaryProtocol(new CarrotBinaryProtocolConfiguration()
                {
                    CommandPacketLength = 256,
                    DataPacketLength = _config.DataPacketLength
                });
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

            // 检查嵌套协议头
            if (!reader.TryPeek(out var startByte))
                return false;

            // 解码嵌套协议
            if (startByte == CarrotBinaryProtocol.StartByte)
                return _innerProtocol.TryDecode(ref buffer, out packet);

            // 检查ascii协议完整包
            if (!reader.TryReadTo(out ReadOnlySequence<byte> seq, (byte)'\n', true))
                return false;

            var payload = Encoding.ASCII.GetString(seq).TrimEnd('\r');

            if (startByte == '[')
            {
                // 数据包(示例: "[DATA]: 0xAA" )
                int colonIndex = payload.IndexOf(':');
                if (colonIndex > 0)
                {
                    string key = payload.Substring(1, colonIndex - 2);
                    string stringValue = payload.Substring(colonIndex + 1).Trim();
                    if (key == "DATA")
                    {
                        if (StringEx.TryToDouble(stringValue, out var doubleValue))
                        {
                            packet = new DataPacket(new double[] { doubleValue });
                            buffer = buffer.Slice(reader.Position);
                            return true;
                        }
                    }
                }
            }
            // 解码ascii命令
            packet = new CommandPacket(CommandPacket.AddLineEnding(payload));

            buffer = buffer.Slice(reader.Position);
            return true;
        }
    }
}