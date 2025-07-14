using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services
{
    public static class DeviceSessionExtensions
    {
        // ASCII 指令
        public static Task SendAscii(this DeviceSession service, string message)
            => service.WriteAsync(new CommandPacket(message));

        // 二进制数据
        public static Task SendBinary(this DeviceSession service, byte[] data)
           => throw new NotImplementedException();
        // => service.WriteAsync(new DataPacket(data));

        // 寄存器操作
        public static Task SendRegister(
            this DeviceSession service,
            RegisterOperation operation,
            int registerFile,
            int address,
            int value
        ) => service.WriteAsync(new RegisterPacket(operation, registerFile, address, value));

        // 寄存器操作
        public static Task SendRegister(
            this DeviceSession service,
            RegisterOperation operation,
            int registerFile,
            int address,
            int startBits,
            int endBits,
            int value
        ) => service.WriteAsync(new RegisterPacket(operation, registerFile, address, startBits, endBits, value));
    }
}
