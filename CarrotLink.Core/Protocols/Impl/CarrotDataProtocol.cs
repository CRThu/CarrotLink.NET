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
        public static new string Version { get; } = "CDPV1";

        public CarrotDataProtocol()
        {
        }

        public override byte[] Pack(IPacket packet)
        {
            return packet switch {
                AsciiPacket p => EncodeAscii(p.Message),
                BinaryPacket p => EncodeBinary(p.Data),
                RegisterPacket p => EncodeRegister(p.Oper, p.RegFile, p.Addr, p.Value),
                _ => throw new NotSupportedException()
            };
        }

        private static byte[] EncodeAscii(string message)
        {
            // 实现 ASCII 封包逻辑（带CDP帧头）
            throw new NotImplementedException(nameof(EncodeAscii) + "is not implemented.");
        }

        private static byte[] EncodeBinary(byte[] data)
        {
            // 实现二进制数据封包逻辑
            throw new NotImplementedException(nameof(EncodeBinary) + "is not implemented.");
        }

        private static byte[] EncodeRegister(int oper, int regFile, int addr, int value)
        {
            // 实现寄存器操作封包逻辑
            throw new NotImplementedException(nameof(EncodeRegister) + "is not implemented.");
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
                    //return false;
                }
                if ((reader.Remaining < 2) || (!reader.TryPeek(1, out byte protocolId)))
                {
                    // 不完整包结构则结束
                    break;
                    //return false;
                }
                if (GetPacketLength(protocolId) == -1)
                {
                    break;
                }
                if ((reader.Remaining < packetLen) || (!reader.TryPeek(packetLen - 1, out byte frameEnd)))
                {
                    // 不完整包结构则结束
                    break;
                    //return false;
                }

                var x = reader.TryReadExact(packetLen, out var pktSeq);
                if (x)
                {
                    CarrotDataProtocolPacket pkt = new(pktSeq.ToArray());
                    switch (GetCdpType(pkt.ProtocolId!.Value))
                    {
                        case CDP_TYPE.DATA:
                            pkt = new CdpDataPacket(pkt);
                            break;
                        case CDP_TYPE.ASCII:
                            pkt = new CdpMessagePacket(pkt);
                            break;
                        case CDP_TYPE.REG:
                            pkt = new CdpRegisterPacket(pkt);
                            break;
                        default:
                            break;
                    }
                    buffer = buffer.Slice(buffer.GetPosition(packetLen));
                    packet = pkt;
                    break;
                }
            }
            return packet != null;
        }

    }
}
