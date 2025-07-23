using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public enum DataType { INT64, INT32, INT16, INT8, FP64 }
    public enum DataEncoding { OffsetBinary, TwosComplement }
    public enum DataEndian { LittleEndian, BigEndian }

    public interface IDataPacket : IPacket
    {
        public DataType Type { get; }
        public DataEncoding Encoding { get; }
        public DataEndian Endian { get; }

        public string[] Keys { get; }
        public byte[] RawData { get; }
        public ReadOnlySpan<T> Get<T>(int channel) where T : unmanaged;
    }

    public record DataPacket : IDataPacket
    {
        public PacketType PacketType => PacketType.Data;

        public DataType Type { get; init; }
        public DataEncoding Encoding { get; init; }
        public DataEndian Endian { get; init; }

        public string[] Keys { get; init; }

        /// <summary>
        /// if has one key, rawdata map: key, key, key, key
        /// if has two keys, rawdata map: key1, key2, key1, key2
        /// if has four keys, rawdata map: key1, key2, key3, key4
        /// </summary>
        public byte[] RawData { get; init; }

        public DataPacket(IEnumerable<double> values)
        {
            Type = DataType.FP64;
            Encoding = DataEncoding.OffsetBinary;
            Endian = DataEndian.LittleEndian;
            Keys = new string[1] { "0" };
            RawData = values.SelectMany(BitConverter.GetBytes).ToArray();
        }

        public DataPacket(DataType type, DataEncoding encoding, DataEndian endian, int channel, IEnumerable<byte> rawData)
        {
            Type = type;
            Encoding = encoding;
            Endian = endian;
            Keys = new string[1] { channel.ToString() };
            RawData = rawData.ToArray();
        }

        public DataPacket(DataType type, DataEncoding encoding, DataEndian endian, IEnumerable<int> channels, IEnumerable<byte> rawData)
        {
            Type = type;
            Encoding = encoding;
            Endian = endian;
            Keys = channels.Select(c => c.ToString()).ToArray();
            RawData = rawData.ToArray();
        }

        public ReadOnlySpan<T> Get<T>(int channel) where T : unmanaged
        {
            // 参数验证
            if (Keys == null)
                throw new ArgumentNullException(nameof(Keys));
            if (Keys.Length != 1)
                throw new NotImplementedException($"当前仅支持单通道数据解析");
            if (RawData == null)
                throw new ArgumentNullException(nameof(RawData));

            if ((Endian == DataEndian.BigEndian) != BitConverter.IsLittleEndian)
                throw new NotImplementedException($"不支持的大小端: {Endian}");

            int channelIndex = Array.IndexOf(Keys, channel.ToString());
            if (channelIndex < 0)
                throw new ArgumentException($"Channel {channel} not found in packet");

            //// 预计算常量
            //int bytesPerValue = GetBytesPerValue(Type);
            //int channelCount = Keys.Length;
            //int totalValues = RawData.Length / (bytesPerValue * channelCount);

            //// 数据完整性检查
            //if (bytesPerValue * channelCount == 0 || RawData.Length % (bytesPerValue * channelCount) != 0)
            //    throw new ArgumentException("Invalid raw data length");

            // 兼容类型检查
            if (IsDirectCast<T>(Type, Encoding))
            {
                return MemoryMarshal.Cast<byte, T>(RawData.AsSpan());
            }

            throw new InvalidCastException($"Type {typeof(T)} is not compatible with data type {Type}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDirectCast<T>(DataType dataType, DataEncoding encoding) where T : unmanaged
        {
            return (dataType, encoding, typeof(T)) switch
            {
                (DataType.INT8, DataEncoding.TwosComplement, Type t) when t == typeof(sbyte) => true,
                (DataType.INT8, DataEncoding.OffsetBinary, Type t) when t == typeof(byte) => true,

                (DataType.INT16, DataEncoding.TwosComplement, Type t) when t == typeof(short) => true,
                (DataType.INT16, DataEncoding.OffsetBinary, Type t) when t == typeof(ushort) => true,

                (DataType.INT32, DataEncoding.TwosComplement, Type t) when t == typeof(int) => true,
                (DataType.INT32, DataEncoding.OffsetBinary, Type t) when t == typeof(uint) => true,

                (DataType.INT64, DataEncoding.TwosComplement, Type t) when t == typeof(long) => true,
                (DataType.INT64, DataEncoding.OffsetBinary, Type t) when t == typeof(ulong) => true,

                (DataType.FP64, _, Type t) when t == typeof(double) => true,

                _ => false
            };
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool CanConvert<T>(DataType dataType, DataEncoding encoding) where T : unmanaged
        {
            return (dataType, encoding, typeof(T)) switch
            {
                (DataType.INT8, DataEncoding.TwosComplement, Type t) when t == typeof(sbyte)
                                        || t == typeof(short)
                                        || t == typeof(ushort)
                                        || t == typeof(int)
                                        || t == typeof(uint)
                                        || t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT8, DataEncoding.OffsetBinary, Type t) when t == typeof(byte)
                                        || t == typeof(short)
                                        || t == typeof(ushort)
                                        || t == typeof(int)
                                        || t == typeof(uint)
                                        || t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT16, DataEncoding.TwosComplement, Type t) when t == typeof(short)
                                        || t == typeof(int)
                                        || t == typeof(uint)
                                        || t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT16, DataEncoding.OffsetBinary, Type t) when t == typeof(ushort)
                                        || t == typeof(int)
                                        || t == typeof(uint)
                                        || t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT32, DataEncoding.TwosComplement, Type t) when t == typeof(int)
                                        || t == typeof(uint)
                                        || t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT32, DataEncoding.OffsetBinary, Type t) when t == typeof(uint)
                                        || t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT64, DataEncoding.TwosComplement, Type t) when t == typeof(long)
                                        || t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.INT64, DataEncoding.OffsetBinary, Type t) when t == typeof(ulong)
                                        || t == typeof(float)
                                        || t == typeof(double) => true,
                (DataType.FP64, DataEncoding.TwosComplement, Type t) when t == typeof(double) => true,
                _ => false
            };
        }
    }
}
