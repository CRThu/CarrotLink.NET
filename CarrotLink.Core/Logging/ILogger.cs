using CarrotLink.Core.Protocols.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Logging
{
    public enum LoggerLevel
    {
        Debug,
        Info,
        Warn,
        Error,
    }

    public interface ILogger
    {

    }

    public interface IPacketLogger
    {
        void HandlePacket(IPacket packet);
    }

    public interface IRuntimeLogger
    {
        void HandleRuntime(string message, LoggerLevel level = LoggerLevel.Info, Exception ex = null);
    }

}
