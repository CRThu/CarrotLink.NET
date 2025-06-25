using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;


namespace CarrotLink.Core.Protocols.Models
{
    public enum PacketType { Command, Data, Register }
    public enum RegisterOperation { Write, ReadRequest, ReadResult, BitsWrite, BitsReadRequest, BitsReadResult }

    public enum DataType { INT64, INT32, INT16, INT8, FP64 }
    public enum DataEncoding { OffsetBinary, TwosComplement }
    public enum DataEndian { LittleEndian, BigEndian }

    public interface IPacket
    {
        public PacketType PacketType { get; }
    }

    public interface ICommandPacket : IPacket
    {
        public string Command { get; }
    }

    public interface IDataPacket : IPacket
    {
        public DataType DataType { get; }
        public DataEncoding Encoding { get; }
        public DataEndian Endian { get; }

        public int[] Channels { get; }
        public byte[] RawData { get; }
        public T[] GetValues<T>(int channel);
    }

    public interface IRegisterPacket : IPacket
    {
        public RegisterOperation Operation { get; }
        public int Regfile { get; }
        public int Address { get; }
        public int StartBits { get; }
        public int EndBits { get; }
        public int Value { get; }
    }

    public record CommandPacket(string Command) : ICommandPacket
    {
        public PacketType PacketType => PacketType.Command;

        public override string ToString() => AddLineEnding(Command);


        private static string AddLineEnding(string cmd)
        {
            return cmd.EndsWith('\n') ? cmd : cmd + "\n";
        }
    }

    public record DataPacket : IDataPacket
    {
        public PacketType PacketType => PacketType.Data;

        public DataType DataType { get; init; }
        public DataEncoding Encoding { get; init; }
        public DataEndian Endian { get; init; }

        public required int[] Channels { get; init; }
        public required byte[] RawData { get; init; }

        private DataPacket(DataType type, DataEncoding encoding, DataEndian endian, int channel, IEnumerable<byte> rawData)
        {
            DataType = type;
            Encoding = encoding;
            Endian = endian;
            Channels = new int[1] { channel };
            RawData = rawData.ToArray();
        }

        private DataPacket(DataType type, DataEncoding encoding, DataEndian endian, IEnumerable<int> channels, IEnumerable<byte> rawData)
        {
            DataType = type;
            Encoding = encoding;
            Endian = endian;
            Channels = channels.ToArray();
            RawData = rawData.ToArray();
        }

        public T[] GetValues<T>(int channel)
        {
            throw new NotImplementedException();
        }
    }

    public record RegisterPacket : IRegisterPacket
    {
        public PacketType PacketType => PacketType.Register;
        public RegisterOperation Operation { get; init; }
        public int Regfile { get; init; }
        public int Address { get; init; }
        public int StartBits { get; init; }
        public int EndBits { get; init; }
        public int Value { get; init; }

        public override string ToString()
        {
            return Operation switch
            {
                RegisterOperation.Write => $"REG{Regfile}.{Address}.WRITE={Value}",
                RegisterOperation.ReadRequest => $"REG{Regfile}.{Address}.READ?",
                RegisterOperation.ReadResult => $"REG{Regfile}.{Address}.READ={Value}",
                RegisterOperation.BitsWrite => $"REG{Regfile}.{Address}.{StartBits}:{EndBits}.WRITE={Value}",
                RegisterOperation.BitsReadRequest => $"REG{Regfile}.{Address}.{StartBits}:{EndBits}.READ?",
                RegisterOperation.BitsReadResult => $"REG{Regfile}.{Address}.{StartBits}:{EndBits}.READ={Value}",
                _ => throw new NotImplementedException()
            };
        }

        public RegisterPacket(RegisterOperation operation, int regfile, int address, int value = default)
        {
            Operation = operation;
            Regfile = regfile;
            Address = address;
            Value = value;
        }

        public RegisterPacket(RegisterOperation operation, int regfile, int address, int startBits, int endBits, int value = default)
        {
            Operation = operation;
            Regfile = regfile;
            Address = address;
            StartBits = startBits;
            EndBits = endBits;
            Value = value;
        }

    }
}
