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

            // 1. 检查嵌套二进制协议
            if (startByte == CarrotBinaryProtocol.StartByte)
                return _innerProtocol.TryDecode(ref buffer, out packet);

            // 2. 检查 ASCII 完整包 (以 \n 结尾)
            if (!reader.TryReadTo(out ReadOnlySequence<byte> seq, (byte)'\n', true))
                return false;

            var payloadSpan = Encoding.ASCII.GetString(seq).AsSpan().TrimEnd('\r');

            // 3. 处理 Carrot RPC 特征格式: [HEADER]: BODY
            if (startByte == '[')
            {
                int rightBracketIndex = payloadSpan.IndexOf(']');
                int colonIndex = payloadSpan.IndexOf(':');

                if (rightBracketIndex > 0 && colonIndex > rightBracketIndex)
                {
                    var headerSpan = payloadSpan.Slice(1, rightBracketIndex - 1);
                    var bodySpan = payloadSpan.Slice(colonIndex + 1).Trim();

                    // 拆分 Header (如: DATA.IMU -> ["DATA", "IMU"])
                    var headerParts = headerSpan.ToString().Split('.', StringSplitOptions.RemoveEmptyEntries);
                    string mainKey = headerParts.Length > 0 ? headerParts[0] : string.Empty;

                    // --- DATA 协议解析 ---
                    if (mainKey.Equals("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        var channels = new List<string>();
                        var values = new List<double>();
                        string path = headerParts.Length > 1 ? headerParts[1] : string.Empty;

                        var remainingBody = bodySpan;
                        int valIdx = 0;
                        while (!remainingBody.IsEmpty)
                        {
                            int commaIdx = remainingBody.IndexOf(',');
                            var chunk = (commaIdx == -1) ? remainingBody : remainingBody.Slice(0, commaIdx);
                            int equalIdx = chunk.IndexOf('=');

                            ReadOnlySpan<char> keySpan = equalIdx != -1 ? chunk.Slice(0, equalIdx).Trim() : default;
                            ReadOnlySpan<char> valSpan = equalIdx != -1 ? chunk.Slice(equalIdx + 1).Trim() : chunk.Trim();

                            if (SpanEx.TryParseNumSpan(valSpan, out double dVal))
                            {
                                // 拼接通道名逻辑: Path.Key 或 Path 或 Key 或 CHn
                                string key = !keySpan.IsEmpty ? keySpan.ToString() :
                                            (!string.IsNullOrEmpty(path) && bodySpan.IndexOf(',') == -1 ? "" : $"CH{valIdx}");

                                string fullName = string.IsNullOrEmpty(path) ? key :
                                                 string.IsNullOrEmpty(key) ? path : $"{path}.{key}";

                                channels.Add(fullName);
                                values.Add(dVal);
                                valIdx++;
                            }
                            if (commaIdx == -1)
                                break;
                            remainingBody = remainingBody.Slice(commaIdx + 1);
                        }

                        if (values.Count > 0)
                        {
                            packet = new DataPacket(channels, values);
                            buffer = buffer.Slice(reader.Position);
                            return true;
                        }
                    }
                    // --- REG / BITS 协议解析 ---
                    else if (mainKey.Equals("REG", StringComparison.OrdinalIgnoreCase))
                    {
                        uint address = 0, start = 0, end = 0;
                        bool hasAddr = false, isBits = false;

                        if (headerParts.Length > 1)
                        {
                            if (SpanEx.TryParseHexSpan(headerParts[1], out var addr64))
                            { address = (uint)addr64; hasAddr = true; }

                            if (headerParts.Length > 2 && headerParts[2].StartsWith("b", StringComparison.OrdinalIgnoreCase))
                            {
                                var bSpan = headerParts[2].AsSpan(1);
                                int uIdx = bSpan.IndexOf('_');
                                if (uIdx != -1 && uint.TryParse(bSpan.Slice(0, uIdx), out end) && uint.TryParse(bSpan.Slice(uIdx + 1), out start))
                                    isBits = true;
                            }
                        }

                        // 解析 Body (强制十六进制)
                        if (SpanEx.TryParseHexSpan(bodySpan, out var hexVal))
                        {
                            if (hasAddr)
                            {
                                packet = isBits ? new RegisterPacket(RegisterOperation.BitsReadResult, 0, address, start, end, (uint)hexVal)
                                               : new RegisterPacket(RegisterOperation.ReadResult, 0, address, (uint)hexVal);
                                buffer = buffer.Slice(reader.Position);
                                return true;
                            }
                            else if (_lastRegisterRequest != null) // 上下文兼容
                            {
                                packet = _lastRegisterRequest.Operation == RegisterOperation.ReadRequest
                                    ? new RegisterPacket(RegisterOperation.ReadResult, _lastRegisterRequest.Regfile, _lastRegisterRequest.Address, (uint)hexVal)
                                    : new RegisterPacket(RegisterOperation.BitsReadResult, _lastRegisterRequest.Regfile, _lastRegisterRequest.Address, _lastRegisterRequest.StartBit, _lastRegisterRequest.EndBit, (uint)hexVal);
                                _lastRegisterRequest = null;
                                buffer = buffer.Slice(reader.Position);
                                return true;
                            }
                        }
                    }
                }
            }

            // 4. 默认解析为 CommandPacket (包含 [INFO]: 这种日志消息)
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