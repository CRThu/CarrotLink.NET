using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error,
    }

    public interface ILogger : IDisposable
    {

    }

    public interface IPacketLogger : ILogger
    {
        void HandlePacket(IPacket packet);
    }

    public interface IRuntimeLogger : ILogger
    {
        void HandleRuntime(string message, LogLevel level = LogLevel.Info, Exception ex = null);
    }

}
