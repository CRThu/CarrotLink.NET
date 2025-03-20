using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CarrotLink.Core.Protocols.Models.CarrotDataProtocolPacket;

namespace CarrotLink.Core.Protocols.Impl
{
    public class CarrotDataProtocol : ProtocolBase
    {
        public static new string Version { get; } = "CDPV1";

        public CarrotDataProtocol()
        {
        }

        protected override bool TryDecode(ref ReadOnlySequence<byte> buffer, out PacketBase? packet)
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
