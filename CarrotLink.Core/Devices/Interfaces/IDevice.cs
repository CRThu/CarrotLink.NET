using CarrotLink.Core.Devices.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Interfaces
{
    public interface IDevice
    {
        bool IsConnected { get; }
        DeviceConfigurationBase Config { get; }

        Task ConnectAsync();
        Task DisconnectAsync();
        Task<int> ReadAsync(Memory<byte> buffer);
        Task WriteAsync(ReadOnlyMemory<byte> data);
    }
}