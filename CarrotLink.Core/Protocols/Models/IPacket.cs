using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    public interface IMessagePacket
    {
        public byte[] Encode(string msg);

        public string Decode(byte[] bytes);
    }

    public interface IRegisterPacket
    {
        public byte[] Encode(int oper, int regfile, int addr, int data);

        public (int control, int regfile, int addr, int data) Decode(byte[] bytes);
    }

    public interface IDataPacket
    {
        public byte[] Encode(byte[] data);

        public byte[] Decode(byte[] data);
    }
}
