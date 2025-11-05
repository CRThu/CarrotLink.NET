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

        private IRegisterPacket? _lastRegisterRequest = null;

        public CarrotAsciiProtocol(CarrotAsciiProtocolConfiguration? config)
        {
            _config = config;
            _innerProtocol = new CarrotBinaryProtocol(new CarrotBinaryProtocolConfiguration()
            {
                CommandPacketLength = 256,
            });
        }

        public byte[] Encode(IPacket packet)
        {
            // 如果上一条指令发送的是REG.R/REG.BR, 保存上下文供回复查询结果使用
            if (packet is IRegisterPacket registerPacket &&
                (registerPacket.Operation == RegisterOperation.ReadRequest
                || registerPacket.Operation == RegisterOperation.BitsReadRequest))
                _lastRegisterRequest = registerPacket;

            return packet switch
            {
                ICommandPacket cmd => Encoding.ASCII.GetBytes(CommandPacket.AddLineEnding(cmd.Command)),
                IRegisterPacket reg => CarrotAsciiProtocolRegisterPacket.EncodeRegister(_config, reg),
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

            var payloadSpan = Encoding.ASCII.GetString(seq).AsSpan().TrimEnd('\r');

            if (startByte == '[')
            {
                // 数据包(示例:
                // [DATA]: 0xAA
                // [DATA]: 25.000
                // [DATA]: 0xAA, 0xBB
                // [DATA.iic_addr=0x44]: 0xAA
                // [DATA.iic_addr=0x44]: T=0xAA, RH=0xBB

                int colonIndex = payloadSpan.IndexOf(':');
                if (colonIndex > 0)
                {
                    // "DATA" or "DATA.iic_addr=0x44"
                    var headerSpan = payloadSpan.Slice(1, colonIndex - 2);
                    // "0xAA" or "0xAA,0xBB" or "T=0xAA, RH=0xBB"
                    var bodySpan = payloadSpan.Slice(colonIndex + 1).TrimStart();

                    // "DATA"
                    var mainKeySpan = headerSpan;
                    // empty or "iic_addr=0x44"
                    var channelPrefixSpan = ReadOnlySpan<char>.Empty;
                    int dotIndex = headerSpan.IndexOf('.');
                    if (dotIndex > 0)
                    {
                        mainKeySpan = headerSpan.Slice(0, dotIndex);
                        channelPrefixSpan = headerSpan.Slice(dotIndex + 1);
                    }

                    if (mainKeySpan.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        var channels = new List<string>();
                        var values = new List<double>();

                        var remainingBody = bodySpan;
                        int valueIndex = 0;
                        while (!remainingBody.IsEmpty)
                        {
                            int commaIndex = remainingBody.IndexOf(',');
                            var chunk = (commaIndex == -1) ? remainingBody : remainingBody.Slice(0, commaIndex);

                            int equalIndex = chunk.IndexOf('=');
                            ReadOnlySpan<char> channelNameSpan;
                            ReadOnlySpan<char> valueSpan;

                            if (equalIndex != -1) // 键值对模式: T=0xAA
                            {
                                // "T"
                                channelNameSpan = chunk.Slice(0, equalIndex).Trim();
                                // "0xAA"
                                valueSpan = chunk.Slice(equalIndex + 1).Trim();
                            }
                            else // 索引模式: 0xAA
                            {
                                // Empty
                                channelNameSpan = ReadOnlySpan<char>.Empty;
                                // "0xAA"
                                valueSpan = chunk.Trim();
                            }

                            if (SpanEx.TryParseNumSpan(valueSpan, out double doubleValue))
                            {
                                string baseChannelName = !channelNameSpan.IsEmpty
                                    ? channelNameSpan.ToString()
                                    : $"CH{valueIndex}";

                                // [DATA]: T=value => T, [DATA]:value => CH0
                                string finalChannelName = baseChannelName;

                                if (!channelPrefixSpan.IsEmpty)
                                {
                                    if (bodySpan.IndexOf(',') == -1 && bodySpan.IndexOf('=') == -1)
                                    {
                                        // [DATA.prefix]: value => prefix
                                        finalChannelName = channelPrefixSpan.ToString();
                                    }
                                    else
                                    {
                                        // [DATA.prefix]: T=value => prefix.T
                                        finalChannelName = $"{channelPrefixSpan.ToString()}.{baseChannelName}";
                                    }
                                }

                                channels.Add(finalChannelName);
                                values.Add(doubleValue);
                                valueIndex++;
                            }

                            if (commaIndex == -1)
                                break;
                            remainingBody = remainingBody.Slice(commaIndex + 1);
                        }

                        if (values.Count != 0)
                        {
                            packet = new DataPacket(channels, values);
                            buffer = buffer.Slice(reader.Position);
                            return true;
                        }
                    }
                    else if (mainKeySpan.Equals("REG", StringComparison.OrdinalIgnoreCase))
                    {
                        // 若上一条发送指令为寄存器请求指令且本次接收到的是寄存器回复指令则使用上下文
                        if (_lastRegisterRequest != null
                            && SpanEx.TryParseHexSpan(bodySpan, out var hexValue))
                        {
                            if (_lastRegisterRequest.Operation == RegisterOperation.ReadRequest)
                                packet = new RegisterPacket(RegisterOperation.ReadResult, _lastRegisterRequest.Regfile, _lastRegisterRequest.Address, (uint)hexValue);
                            else if (_lastRegisterRequest.Operation == RegisterOperation.BitsReadRequest)
                                packet = new RegisterPacket(RegisterOperation.BitsReadResult, _lastRegisterRequest.Regfile, _lastRegisterRequest.Address, _lastRegisterRequest.StartBit, _lastRegisterRequest.EndBit, (uint)hexValue);
                            else
                                throw new NotImplementedException("unknown register request & result when decoding.");
                            _lastRegisterRequest = null;
                            buffer = buffer.Slice(reader.Position);
                            return true;
                        }
                    }
                }
            }

            // 解码ascii命令
            packet = new CommandPacket(CommandPacket.AddLineEnding(payloadSpan.ToString()));

            buffer = buffer.Slice(reader.Position);
            return true;
        }
    }
    public static class CarrotAsciiProtocolRegisterPacket
    {
        public static byte[] EncodeRegister(CarrotAsciiProtocolConfiguration config, IRegisterPacket packet)
        {
            if (packet.Regfile >= config.RegfilesCommands.Length)
                throw new ArgumentOutOfRangeException($"regfile({packet.Regfile}) is out of range(0-{config.RegfilesCommands.Length - 1}).");

            var rfCmds = config.RegfilesCommands[packet.Regfile];
            string wrapper_first = config.CommandWrapper == CommandWrapper.Func ? "(" : ";";
            string wrapper_mid = config.CommandWrapper == CommandWrapper.Func ? "," : ";";
            string wrapper_last = config.CommandWrapper == CommandWrapper.Func ? ");" : ";";
            string terminal = "\r\n";
            string cmd = packet.Operation switch
            {
                RegisterOperation.Write =>
                    /* REG.W(ADDR,VAL); */
                    $"{rfCmds.WriteRegCommand}{wrapper_first}" +
                    $"{packet.Address:X}{wrapper_mid}" +
                    $"{packet.Value:X}{wrapper_last}" +
                    terminal,
                RegisterOperation.ReadRequest =>
                    /* REG.R(ADDR); */
                    $"{rfCmds.ReadRegCommand}{wrapper_first}" +
                    $"{packet.Address:X}{wrapper_last}" +
                    terminal,
                RegisterOperation.BitsWrite =>
                    /* REG.BW(ADDR,START,END,VAL); */
                    $"{rfCmds.WriteBitsCommand}{wrapper_first}" +
                    $"{packet.Address:X}{wrapper_mid}" +
                    $"{packet.StartBit}{wrapper_mid}" +
                    $"{packet.EndBit}{wrapper_mid}" +
                    $"{packet.Value:X}{wrapper_last}" +
                    terminal,
                RegisterOperation.BitsReadRequest =>
                    /* REG.BR(ADDR,START,END); */
                    $"{rfCmds.ReadBitsCommand}{wrapper_first}" +
                    $"{packet.Address:X}{wrapper_mid}" +
                    $"{packet.StartBit}{wrapper_mid}" +
                    $"{packet.EndBit}{wrapper_last}" +
                    terminal,
                _ => throw new NotImplementedException(),
            };

            return Encoding.ASCII.GetBytes(cmd.ToString());
        }

        public static IRegisterPacket DecodeRegister(CarrotAsciiProtocolConfiguration config, byte[] payload)
        {
            throw new NotImplementedException();
        }
    }
}