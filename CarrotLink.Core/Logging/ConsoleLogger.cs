using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Logging
{
    public class ConsoleLogger : LoggerBase
    {
        public override void LogInfo(string message)
        => Console.WriteLine(FormatMessage("INFO", message));

        public override void LogInfo(IPacket packet)
            => Console.WriteLine(FormatMessage("INFO", packet.ToString()!));

        public override void LogError(string message, Exception? ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(FormatMessage("ERROR", $"{message} {ex?.Message}"));
            Console.ResetColor();
        }

        public override void LogDebug(string message)
        => Console.WriteLine(FormatMessage("DEBUG", message));

    }
}