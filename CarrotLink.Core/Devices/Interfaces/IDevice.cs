using CarrotLink.Core.Devices.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Interfaces
{
    public interface IDevice : IDisposable
    {
        bool IsConnected { get; }
        DeviceConfigurationBase Config { get; }

        public long TotalReceivedBytes { get; }
        public long TotalSentBytes { get; }

        Task ConnectAsync();
        Task DisconnectAsync();
        Task<int> ReadAsync(Memory<byte> buffer);
        Task WriteAsync(ReadOnlyMemory<byte> data);
    }
}