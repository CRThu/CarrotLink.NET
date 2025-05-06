using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Logging
{
    public abstract class LoggerBase : ILogger
    {
        public abstract void LogInfo(string message);
        public abstract void LogError(string message, Exception? ex = null);
        public abstract void LogDebug(string message);
        protected string FormatMessage(string type, string message)
        => $"[{DateTime.Now:HH:mm:ss.fff}] {type.ToUpper()}: {message}";
    }
}
