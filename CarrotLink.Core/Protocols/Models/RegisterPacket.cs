﻿using System;
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
        public int Regfile { get; }
        public int Address { get; }
        public int StartBits { get; }
        public int EndBits { get; }
        public int Value { get; }
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
