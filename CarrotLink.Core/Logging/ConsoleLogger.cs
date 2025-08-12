using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Logging
{
    public class ConsoleLogger : ILogger, IPacketLogger, IRuntimeLogger
    {
        private void Log(LogLevel type, string message)
        => Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {type.ToString().ToUpper()}: {message}");

        public void HandleRuntime(string message, LogLevel level = LogLevel.Info, Exception ex = null)
        {
            if (ex == null)
            {
                Log(level, message);
            }
            else
            {
                Log(level, $"{message}\r\n{ex}");
            }
        }

        public void HandlePacket(IPacket packet)
        {
            Log(LogLevel.Info, packet.ToString() ?? "<null>");
        }

        public void Dispose()
        {
        }
    }
}