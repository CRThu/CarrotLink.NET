using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services
{
    public static class DeviceServiceExtensions
    {
        // ASCII 指令（自动追加 CRLF）
        public static Task SendAscii(this DeviceService service, string message)
            => service.WriteAsync(new AsciiPacket(message));

        // 二进制数据（直接传递 byte[]）
        public static Task SendBinary(this DeviceService service, byte[] data)
            => service.WriteAsync(new BinaryPacket(data));

        // 寄存器操作（参数命名明确）
        public static Task SendRegister(
            this DeviceService service,
            int operation,
            int registerFile,
            int address,
            int value
        ) => service.WriteAsync(new RegisterRawPacket(operation, registerFile, address, value));
    }
}
