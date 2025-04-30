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

        public long TotalReadBytes { get; }
        public long TotalWriteBytes { get; }

        Task ConnectAsync(CancellationToken cancellationToken = default);
        Task DisconnectAsync(CancellationToken cancellationToken = default);
        Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);
        Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);
    }
}