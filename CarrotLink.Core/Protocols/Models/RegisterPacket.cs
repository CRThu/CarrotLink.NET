using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public enum RegisterOperation { Write, ReadRequest, ReadResult, BitsWrite, BitsReadRequest, BitsReadResult }

    public interface IRegisterPacket : IPacket
    {
        public RegisterOperation Operation { get; }
        public uint Regfile { get; }
        public uint Address { get; }
        public uint StartBit { get; }
        public uint EndBit { get; }
        public uint Value { get; }
    }

    public record RegisterPacket : IRegisterPacket
    {
        public PacketType PacketType => PacketType.Register;
        public RegisterOperation Operation { get; init; }
        public uint Regfile { get; init; }
        public uint Address { get; init; }
        public uint StartBit { get; init; }
        public uint EndBit { get; init; }
        public uint Value { get; init; }

        public override string ToString()
        {
            return Operation switch
            {
                RegisterOperation.Write => $"REG{Regfile}.{Address}.WRITE={Value}",
                RegisterOperation.ReadRequest => $"REG{Regfile}.{Address}.READ?",
                RegisterOperation.ReadResult => $"REG{Regfile}.{Address}.READ={Value}",
                RegisterOperation.BitsWrite => $"REG{Regfile}.{Address}.{StartBit}:{EndBit}.WRITE={Value}",
                RegisterOperation.BitsReadRequest => $"REG{Regfile}.{Address}.{StartBit}:{EndBit}.READ?",
                RegisterOperation.BitsReadResult => $"REG{Regfile}.{Address}.{StartBit}:{EndBit}.READ={Value}",
                _ => throw new NotImplementedException()
            };
        }

        public RegisterPacket(RegisterOperation operation, uint regfile, uint address, uint value = default)
        {
            Operation = operation;
            Regfile = regfile;
            Address = address;
            Value = value;
        }

        public RegisterPacket(RegisterOperation operation, uint regfile, uint address, uint startBit, uint endBit, uint value = default)
        {
            Operation = operation;
            Regfile = regfile;
            Address = address;
            StartBit = startBit;
            EndBit = endBit;
            Value = value;
        }

    }
}
