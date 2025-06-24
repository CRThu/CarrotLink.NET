using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
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

        public const byte StartByte = 0x3C;
        public const byte EndByte = 0x3E;

        public const byte Command64PacketId = 0x31;
        public const byte Command256PacketId = 0x32;
        public const byte Command2048PacketId = 0x33;
        public const byte Data64PacketId = 0x41;
        public const byte Data256PacketId = 0x42;
        public const byte Data2048PacketId = 0x43;
        public const byte RegisterRequestPacketId = 0xA0;
        public const byte RegisterReplyPacketId = 0xA8;

        public byte CommandPacketId { get; init; }
        public byte DataPacketId { get; init; }


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
                IRegisterPacket reg => CarrotDataProtocolRegisterPacket.EncodeRegister(reg),
                _ => Array.Empty<byte>(),
            };

            return CreateFrame(protocolId, payload);
        }

        public override bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
        {
            packet = null;

            // 最小帧长度64
            if (buffer.Length < 64)
                return false;

            SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
            int packetLength;

            // 读取帧头
            if (!reader.TryRead(out byte startByte))
                return false;
            if (startByte != StartByte)
                throw new InvalidDataException($"Cannot decode packet with start byte: 0x{startByte:02X}(expect 0x{StartByte:02X}).");
            // 读取协议id
            if (!reader.TryRead(out byte protocolId))
                return false;
            // 协议长度计算
            packetLength = GetFrameLength(protocolId);
            if (buffer.Length < packetLength)
                return false;
            // 跳过控制位
            reader.Advance(2);
            // 读取payload长度
            if (!reader.TryReadExact(2, out var payloadLengthSeq))
                return false;
            ushort payloadLength = BitConverter.ToUInt16(payloadLengthSeq.ToArray());
            // 读取payload(payload实际长度为payloadLength,完整payload长度为packetLength-10)
            var payloadSeq = buffer.Slice(reader.Position, payloadLength);
            byte[] payload = payloadSeq.ToArray();
            reader.Advance(packetLength - 10);
            // CRC
            reader.Advance(2);
            // 帧尾
            if (!reader.TryRead(out byte endByte))
                return false;
            if (endByte != EndByte)
                throw new InvalidDataException($"Cannot decode packet with end byte: 0x{endByte:02X}(expect 0x{EndByte:02X}).");
            // 创建包
            switch (protocolId)
            {
                case Command64PacketId:
                case Command256PacketId:
                case Command2048PacketId:
                    packet = new CommandPacket(Encoding.ASCII.GetString(payload));
                    break;
                case Data64PacketId:
                case Data256PacketId:
                case Data2048PacketId:
                    packet = new DataPacket(payload);
                    break;
                case RegisterRequestPacketId:
                case RegisterReplyPacketId:
                    packet = CarrotDataProtocolRegisterPacket.DecodeRegister(payload);
                    break;
                default:
                    throw new NotImplementedException();
            }
            // 更新缓冲区
            buffer = buffer.Slice(reader.Position);
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

    }

    public static class CarrotDataProtocolDataPacket
    {
        public static byte[] EncodeData(IDataPacket packet)
        {
            throw new NotImplementedException();
        }

        public static IDataPacket DecodeData(byte[] payload)
        {
            throw new NotImplementedException();
        }
    }

    public static class CarrotDataProtocolRegisterPacket
    {
        public static byte[] EncodeRegister(IRegisterPacket packet)
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

        public static IRegisterPacket DecodeRegister(byte[] payload)
        {
            if (payload.Length == 16)
                return new RegisterPacket(
                    (RegisterOperation)BitConverter.ToInt32(payload, 0),
                    BitConverter.ToInt32(payload, 4),
                    BitConverter.ToInt32(payload, 8),
                    BitConverter.ToInt32(payload, 12)
                    );
            else
                return new RegisterPacket(
                    (RegisterOperation)BitConverter.ToUInt32(payload, 0),
                    BitConverter.ToInt32(payload, 4),
                    BitConverter.ToInt32(payload, 8),
                    BitConverter.ToInt32(payload, 12),
                    BitConverter.ToInt32(payload, 16),
                    BitConverter.ToInt32(payload, 20)
                    );

        }
    }
}
