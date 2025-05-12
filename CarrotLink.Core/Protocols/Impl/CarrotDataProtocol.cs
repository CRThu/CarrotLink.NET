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
        public static new string Version { get; } = "CDPV1";

        public CarrotDataProtocol()
        {
        }

        public const byte CDP_PACKET_START_BYTE = 0x3C;
        public const byte CDP_PACKET_END_BYTE = 0x3E;

        public const byte ProtocolIdAsciiTransfer64 = 0x31;
        public const byte ProtocolIdAsciiTransfer256 = 0x32;
        public const byte ProtocolIdAsciiTransfer2048 = 0x33;
        public const byte ProtocolIdDataTransfer74 = 0x41;
        public const byte ProtocolIdDataTransfer266 = 0x42;
        public const byte ProtocolIdDataTransfer2058 = 0x43;
        public const byte ProtocolIdRegisterOper = 0xA0;
        public const byte ProtocolIdRegisterReply = 0xA8;

        /// <summary>
        /// 预设协议长度
        /// </summary>
        /// <param name="ProtocolId"></param>
        /// <returns></returns>
        public static int GetPacketLength(byte protocolId)
        {
            return protocolId switch
            {

                ProtocolIdAsciiTransfer64 => 64,
                ProtocolIdAsciiTransfer256 => 256,
                ProtocolIdAsciiTransfer2048 => 2048,
                ProtocolIdDataTransfer74 => 64 + 10,
                ProtocolIdDataTransfer266 => 256 + 10,
                ProtocolIdDataTransfer2058 => 2048 + 10,
                ProtocolIdRegisterOper => 256,
                ProtocolIdRegisterReply => 256,
                _ => -1,
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


        public override byte[] GetBytes(IPacket packet)
        {
            return packet switch
            {
                AsciiPacket p => EncodeAscii(p.Payload),
                BinaryPacket p => EncodeBinary(p.Payload),
                RegisterPacket p => EncodeRegister(p.Payload._oper, p.Payload._regfile, p.Payload._addr, p.Payload._value),
                _ => throw new NotSupportedException()
            };
        }

        private static byte[] EncodeAscii(string message)
        {
            // 实现 ASCII 封包逻辑（带CDP帧头）
            CdpMessagePacket p = new(message);
            return p.Bytes!;
        }

        private static byte[] EncodeBinary(byte[] data)
        {
            // 实现二进制数据封包逻辑
            CdpDataPacket p = new(data);
            return p.Bytes!;
        }

        private static byte[] EncodeRegister(int oper, int regFile, int addr, int value)
        {
            // 实现寄存器操作封包逻辑
            CdpRegisterPacket p = new(oper, regFile, addr, value);
            return p.Bytes!;
        }


        protected override bool TryDecode(ref ReadOnlySequence<byte> buffer, out IPacket? packet)
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
                packetLen = GetPacketLength(protocolId);
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

    }
}
