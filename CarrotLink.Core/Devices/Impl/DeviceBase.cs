using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices.Impl
{
    public abstract class DeviceBase<TConfig> : IDevice, IDisposable where TConfig : DeviceConfigurationBase
    {
        public TConfig Config { get; }
        public bool IsConnected { get; protected set; }
        DeviceConfigurationBase IDevice.Config => Config;

        public long TotalReceivedBytes { get; protected set; } = 0;
        public long TotalSentBytes { get; protected set; } = 0;

        public DeviceBase(TConfig config) => Config = config;

        protected CancellationToken CreateTimeoutToken()
        {
            var cts = new CancellationTokenSource();
            if (Config.Timeout > 0)
            {
                cts.CancelAfter(Config.Timeout);
            }
            return cts.Token;
        }

        public abstract Task ConnectAsync(CancellationToken cancellationToken = default);
        public abstract Task DisconnectAsync(CancellationToken cancellationToken = default);

        public abstract Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

        public abstract Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

        public virtual void Dispose() => DisconnectAsync().Wait();
    }
}
