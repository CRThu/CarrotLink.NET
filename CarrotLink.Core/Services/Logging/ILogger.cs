using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Logging
{
    public interface ILogger
    {
        void LogInfo(string message);
        void LogError(string message, Exception? ex = null);
        void LogDebug(string message);
    }
}
