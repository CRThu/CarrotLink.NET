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
        protected TConfig _config;
        protected long _totalReadBytes = 0;
        protected long _totalWriteBytes = 0;

        public virtual bool IsConnected { get; protected set; }
        public DeviceConfigurationBase Config => _config;

        public long TotalReadBytes => _totalReadBytes;
        public long TotalWriteBytes => _totalWriteBytes;

        public DeviceBase(TConfig config)
        {
            _config = config;
        }

        protected CancellationToken CreateTimeoutToken()
        {
            var cts = new CancellationTokenSource();
            if (_config.Timeout > 0)
            {
                cts.CancelAfter(_config.Timeout);
            }
            return cts.Token;
        }

        public abstract void Connect();
        public abstract void Disconnect();

        public abstract Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default);

        public abstract Task WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

        public virtual void Dispose()
        {
        }
    }
}
