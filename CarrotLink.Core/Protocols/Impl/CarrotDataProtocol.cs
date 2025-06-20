using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Protocols.Models.Old;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    public enum CDP_TYPE
    {
        UNKNOWN = 0x00,
        ASCII = 0x30,
        DATA = 0x40,
        REG = 0xA0
    };

    public class CarrotDataProtocol : ProtocolBase
    {
        public CarrotDataProtocol(int cmdlen = 256, int datalen = 256)
        {
            CommandPacketId = cmdlen switch
            {
                64 => Command64PacketId,
                256 => Command256PacketId,
                2048 => Command2048PacketId,
                _ => throw new NotImplementedException()
            };
            CommandPacketId = datalen switch
            {
                64 => Data64PacketId,
                256 => Data256PacketId,
                2048 => Data2048PacketId,
                _ => throw new NotImplementedException()
            };
        }

        private const byte StartByte = 0x3C;
        private const byte EndByte = 0x3E;

        private const byte Command64PacketId = 0x31;
        private const byte Command256PacketId = 0x32;
        private const byte Command2048PacketId = 0x33;
        private const byte Data64PacketId = 0x41;
        private const byte Data256PacketId = 0x42;
        private const byte Data2048PacketId = 0x43;
        private const byte RegisterRequestPacketId = 0xA0;
        private const byte RegisterReplyPacketId = 0xA8;

        private byte CommandPacketId { get; init; }
        private byte DataPacketId { get; init; }


        public override string ProtocolName => nameof(CarrotDataProtocol);

        public override int ProtocolVersion => 0;

        private static int GetFrameLength(byte protocolId)
        {
            return protocolId switch
            {
                Command64PacketId => 64,
                Command256PacketId => 256,
                Command2048PacketId => 2048,
                Data64PacketId => 64 + 10,
                Data256PacketId => 256 + 10,
                Data2048PacketId => 2048 + 10,
                RegisterRequestPacketId => 256,
                RegisterReplyPacketId => 256,
                _ => throw new NotSupportedException($"Unsupported protocol id: {protocolId}")
            };
        }

        public static CDP_TYPE GetCdpType(byte protocolId)
        {
            if (protocolId >= 0x30 && protocolId <= 0x3F)
                return CDP_TYPE.ASCII;
            else if (protocolId >= 0x40 && protocolId <= 0x4F)
                return CDP_TYPE.DATA;
            else if (protocolId >= 0xA0 && protocolId <= 0xAF)
                return CDP_TYPE.REG;
            else
                return CDP_TYPE.UNKNOWN;
        }


        public override byte[] Encode(IPacket packet)
        {
            byte protocolId = packet switch
            {
                ICommandPacket => CommandPacketId,
                IDataPacket => DataPacketId,
                IRegisterPacket => RegisterRequestPacketId,
                _ => throw new NotSupportedException($"Unsupported packet type: {packet.Type}")
            };

            byte[] payload = packet switch
            {
                ICommandPacket cmd => Encoding.ASCII.GetBytes(cmd.Command),
                IDataPacket data => data.Payload,
                IRegisterPacket reg => EncodeRegister(reg),
                _ => Array.Empty<byte>(),
            };

            return CreateFrame(protocolId, payload);
        }

        public override bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
        {
            packet = null;
            SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
            int packetLen = 0;

            // 处理数据流直到不完整包或结束
            while (true)
            {
                // 读取帧头
                if ((reader.Remaining < 1) || (!reader.TryPeek(0, out byte frameStart)))
                {
                    // 不完整包结构则结束
                    break;
                }
                if ((reader.Remaining < 2) || (!reader.TryPeek(1, out byte protocolId)))
                {
                    // 不完整包结构则结束
                    break;
                }
                packetLen = GetFrameLength(protocolId);
                if (packetLen == -1)
                {
                    break;
                }
                if ((reader.Remaining < packetLen) || (!reader.TryPeek(packetLen - 1, out byte frameEnd)))
                {
                    // 不完整包结构则结束
                    break;
                }

                var x = reader.TryReadExact(packetLen, out var pktSeq);
                if (x)
                {
                    var pktArray = pktSeq.ToArray();
                    switch (GetCdpType(pktArray[1]))
                    {
                        case CDP_TYPE.DATA:
                            packet = new BinaryPacket(pktArray, PacketType.Data);
                            break;
                        case CDP_TYPE.ASCII:
                            packet = new BinaryPacket(pktArray, PacketType.Command);
                            break;
                        case CDP_TYPE.REG:
                            packet = new BinaryPacket(pktArray, PacketType.Command);
                            break;
                        default:
                            break;
                    }
                    buffer = buffer.Slice(buffer.GetPosition(packetLen));
                    break;
                }
            }
            return packet != null;
        }

        private byte[] CreateFrame(byte protocolId, byte[] payload)
        {
            byte[] frame = new byte[GetFrameLength(protocolId)];
            if (payload.Length > frame.Length - 7 - 3)
                throw new IndexOutOfRangeException($"payload is too long, payload length = {payload.Length}, limit = {frame.Length - 7 - 3}");
            using var writer = new MemoryStream(frame);

            writer.WriteByte(StartByte);
            writer.WriteByte(protocolId);
            writer.Write(BitConverter.GetBytes((ushort)0x0000));
            writer.WriteByte(0);
            writer.Write(BitConverter.GetBytes((ushort)payload.Length));
            writer.Write(payload);
            writer.Write(new byte[frame.Length - 7 - payload.Length - 3]);
            writer.Write(BitConverter.GetBytes((ushort)0xCCCC));
            writer.WriteByte(EndByte);

            return frame;
        }

        private byte[] EncodeRegister(IRegisterPacket packet)
        {
            int payloadLength = packet.Operation switch
            {
                RegisterOperation.Write => 16,
                RegisterOperation.ReadRequest => 16,
                RegisterOperation.ReadResult => 16,
                RegisterOperation.BitsWrite => 24,
                RegisterOperation.BitsReadRequest => 24,
                RegisterOperation.BitsReadResult => 24,
                _ => throw new NotImplementedException(),
            };

            int operationCmd = packet.Operation switch
            {
                RegisterOperation.Write => 0x00,
                RegisterOperation.ReadRequest => 0x01,
                RegisterOperation.ReadResult => 0x01,
                RegisterOperation.BitsWrite => 0x10,
                RegisterOperation.BitsReadRequest => 0x11,
                RegisterOperation.BitsReadResult => 0x11,
                _ => throw new NotImplementedException(),
            };

            byte[] payload = new byte[payloadLength];
            if (payloadLength == 16)
            {
                BitConverter.TryWriteBytes(payload.AsSpan(0, 4), (uint)operationCmd);
                BitConverter.TryWriteBytes(payload.AsSpan(4, 4), (uint)packet.Regfile);
                BitConverter.TryWriteBytes(payload.AsSpan(8, 4), (uint)packet.Address);
                BitConverter.TryWriteBytes(payload.AsSpan(12, 4), (uint)packet.Value);
            }
            else if (payloadLength == 24)
            {
                BitConverter.TryWriteBytes(payload.AsSpan(0, 4), (uint)operationCmd);
                BitConverter.TryWriteBytes(payload.AsSpan(4, 4), (uint)packet.Regfile);
                BitConverter.TryWriteBytes(payload.AsSpan(8, 4), (uint)packet.Address);
                BitConverter.TryWriteBytes(payload.AsSpan(12, 4), (uint)packet.StartBits);
                BitConverter.TryWriteBytes(payload.AsSpan(16, 4), (uint)packet.EndBits);
                BitConverter.TryWriteBytes(payload.AsSpan(20, 4), (uint)packet.Value);
            }
            else
            {
                throw new NotImplementedException();
            }
            return payload;
        }

        private IRegisterPacket DecodeRegister(byte[] payload)
        {
            return new RegisterPacket(

                );
        }
    }

    public static class CarrotDataProtocolRegisterPacket
    {
    }
}
