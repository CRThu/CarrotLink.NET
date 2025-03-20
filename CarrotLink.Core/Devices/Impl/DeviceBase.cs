using CarrotLink.Core.Devices.Configuration;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Impl
{
    public abstract class DeviceBase<TConfig> : IDisposable where TConfig : DeviceConfigurationBase
    {
        public TConfig Config { get; }
        public bool IsConnected { get; protected set; }

        public DeviceBase(TConfig config) => Config = config;

        public abstract Task ConnectAsync();
        public abstract Task DisconnectAsync();

        public abstract Task<int> ReadAsync(Memory<byte> buffer);

        public abstract Task WriteAsync(ReadOnlyMemory<byte> data);

        public virtual void Dispose() => DisconnectAsync().Wait();
    }
}
