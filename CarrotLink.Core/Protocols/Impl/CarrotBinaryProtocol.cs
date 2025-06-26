using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Impl
{
    public class CarrotBinaryProtocol : ProtocolBase
    {
        public CarrotBinaryProtocol(int cmdlen = 256, int datalen = 256)
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


        public override string ProtocolName => nameof(CarrotBinaryProtocol);

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
                _ => throw new NotSupportedException($"Unsupported packet type: {packet.PacketType}")
            };

            byte[] payload = packet switch
            {
                ICommandPacket cmd => Encoding.ASCII.GetBytes(cmd.Command),
                IDataPacket data => CarrotBinaryProtocolDataPacket.EncodeData(data),
                IRegisterPacket reg => CarrotBinaryProtocolRegisterPacket.EncodeRegister(reg),
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
            // 读取控制位和streamId
            if (!reader.TryReadExact(3, out var controlFlagsAndStreamIdSeq))
                return false;
            byte[] controlFlagsAndStreamId = controlFlagsAndStreamIdSeq.ToArray();
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
                    packet = CarrotBinaryProtocolDataPacket.DecodeData(payload, controlFlagsAndStreamId);
                    break;
                case RegisterRequestPacketId:
                case RegisterReplyPacketId:
                    packet = CarrotBinaryProtocolRegisterPacket.DecodeRegister(payload);
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

    public static class CarrotBinaryProtocolDataPacket
    {
        public static class DataTypeConverter
        {
            public static int ToFlag(DataType type)
            {
                return type switch
                {
                    DataType.INT32 => 0x00,
                    DataType.INT16 => 0x01,
                    DataType.INT8 => 0x02,
                    _ => throw new NotImplementedException(),
                };
            }

            public static DataType FromFlag(int flag)
            {
                return flag switch
                {
                    0x00 => DataType.INT32,
                    0x01 => DataType.INT16,
                    0x02 => DataType.INT8,
                    _ => throw new NotImplementedException(),
                };
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct DataPacketConfig
        {
            [FieldOffset(0)] private byte StreamIdByte;
            [FieldOffset(0)] private byte FlagsLowByte;
            [FieldOffset(0)] private byte FlagsHighByte;

            public DataPacketConfig(byte[] flags)
            {
                if (flags == null || flags.Length != 3)
                    throw new ArgumentException("flags is not 3 bytes data");
                StreamIdByte = flags[0];
                FlagsLowByte = flags[1];
                FlagsHighByte = flags[2];
            }

            /// <summary>
            /// FlagH[0]: Interleaved
            /// </summary>
            public bool IsInterleaved
            {
                get => (FlagsHighByte & 0x01) != 0;
                set => FlagsHighByte = (byte)(value ? (FlagsHighByte | 0x01) : (FlagsHighByte & 0xFE));
            }

            /// <summary>
            /// FlagH[1]: IsBigEndian
            /// </summary>
            public bool IsBigEndian
            {
                get => (FlagsHighByte & 0x02) != 0;
                set => FlagsHighByte = (byte)(value ? (FlagsHighByte | 0x02) : (FlagsHighByte & 0xFD));
            }

            public DataEndian Endian
            {
                get => IsBigEndian ? DataEndian.BigEndian : DataEndian.LittleEndian;
                set => IsBigEndian = (value == DataEndian.BigEndian);
            }

            /// <summary>
            /// FlagH[2]: IsTwosComplement
            /// </summary>
            public bool IsTwosComplement
            {
                get => (FlagsHighByte & 0x04) != 0;
                set => FlagsHighByte = (byte)(value ? (FlagsHighByte | 0x04) : (FlagsHighByte & 0xFB));
            }

            public DataEncoding Encoding
            {
                get => IsTwosComplement ? DataEncoding.TwosComplement : DataEncoding.OffsetBinary;
                set => IsTwosComplement = (value == DataEncoding.TwosComplement);
            }

            /// <summary>
            /// FlagH[4:3]: DataWidth, 0:32b, 1:16b, 2:8b
            /// </summary>
            public DataType DataType
            {
                get => DataTypeConverter.FromFlag((FlagsHighByte & 0x18) >> 3);
                set => FlagsHighByte = (byte)((FlagsHighByte & 0xE7) | (DataTypeConverter.ToFlag(value) << 3));
            }

            /// <summary>
            /// InterleavedStreamsIdMask: stream id mask when interleaved=1
            /// </summary>
            public ushort InterleavedStreamsIdMask
            {
                get => (ushort)(FlagsLowByte << 8 | StreamIdByte);
                set
                {
                    FlagsLowByte = (byte)(value >> 8);
                    StreamIdByte = (byte)(value & 0xFF);
                }
            }

            /// <summary>
            /// StreamsId: interleaved ? [flag streamid] mask : streamid
            /// </summary>
            public int[] StreamsId
            {
                get
                {
                    if (!IsInterleaved)
                    {
                        return new int[] { StreamIdByte };
                    }
                    else
                    {
                        var channels = new List<int>(16);
                        ushort mask = InterleavedStreamsIdMask;
                        for (int i = 0; i < 16; i++)
                        {
                            if ((mask & (1 << i)) != 0)
                                channels.Add(i);
                        }
                        return channels.ToArray();
                    }
                }
                set
                {
                    if (!IsInterleaved)
                    {
                        if (value.Length != 1)
                            throw new ArgumentException($"try to set {value.Length} channels in interleaved mode.");

                        if (value[0] > 0 && value[0] < 256)
                        {
                            StreamIdByte = (byte)value[0];
                        }
                        else
                            throw new ArgumentException($"channel {value[0]} is out of range");
                    }
                    else
                    {
                        ushort mask = 0;
                        for (int i = 0; i < value.Length; i++)
                        {
                            if (value[i] > 0 && value[i] < 16)
                            {
                                mask |= (ushort)(1 << value[i]);
                            }
                            else
                                throw new ArgumentException($"channel {value[i]} is out of range");
                        }
                    }
                }
            }
        }

        public static byte[] EncodeData(IDataPacket packet)
        {
            return packet.RawData;
        }

        public static IDataPacket DecodeData(byte[] payload, byte[] controlFlagsAndStreamId)
        {
            DataPacketConfig dpc = new DataPacketConfig(controlFlagsAndStreamId);
            var dataPacket = new DataPacket(dpc.DataType, dpc.Encoding, dpc.Endian, dpc.StreamsId, payload);
            return dataPacket;
        }
    }

    public static class CarrotBinaryProtocolRegisterPacket
    {
        public static class RegisterOperationConverter
        {
            public static uint ToCmd(RegisterOperation operation)
            {
                return operation switch
                {
                    RegisterOperation.Write => 0x00,
                    RegisterOperation.ReadRequest => 0x01,
                    RegisterOperation.ReadResult => 0x01,
                    RegisterOperation.BitsWrite => 0x10,
                    RegisterOperation.BitsReadRequest => 0x11,
                    RegisterOperation.BitsReadResult => 0x11,
                    _ => throw new NotImplementedException(),
                };
            }

            public static RegisterOperation FromCmd(uint cmd, bool isMaster)
            {
                return cmd switch
                {
                    0x00 => RegisterOperation.Write,
                    0x01 => isMaster ? RegisterOperation.ReadRequest : RegisterOperation.ReadResult,
                    0x10 => RegisterOperation.BitsWrite,
                    0x11 => isMaster ? RegisterOperation.BitsReadRequest : RegisterOperation.BitsReadResult,
                    _ => throw new NotImplementedException(),
                };
            }
        }

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

            uint operationCmd = RegisterOperationConverter.ToCmd(packet.Operation);

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
                    RegisterOperationConverter.FromCmd(BitConverter.ToUInt32(payload, 0), false),
                    BitConverter.ToInt32(payload, 4),
                    BitConverter.ToInt32(payload, 8),
                    BitConverter.ToInt32(payload, 12)
                    );
            else
                return new RegisterPacket(
                    RegisterOperationConverter.FromCmd(BitConverter.ToUInt32(payload, 0), false),
                    BitConverter.ToInt32(payload, 4),
                    BitConverter.ToInt32(payload, 8),
                    BitConverter.ToInt32(payload, 12),
                    BitConverter.ToInt32(payload, 16),
                    BitConverter.ToInt32(payload, 20)
                    );
        }
    }
}
