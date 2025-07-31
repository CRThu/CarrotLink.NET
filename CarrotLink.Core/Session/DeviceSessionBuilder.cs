using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Logging;
using CarrotLink.Core.Protocols.Impl;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Session
{
    public class DeviceSessionBuilder
    {
        private IDevice _device;
        private IProtocol _protocol;
        private List<IPacketLogger> _loggers = new List<IPacketLogger>();
        private bool _isAutoPollingEnabled = true;
        private int _pollingInterval = 15;

        public DeviceSessionBuilder WithDevice(IDevice device)
        {
            _device = device;
            return this;
        }

        public DeviceSessionBuilder WithProtocol(IProtocol protocol)
        {
            _protocol = protocol;
            return this;
        }

        public DeviceSessionBuilder WithLogger(IPacketLogger logger)
        {
            _loggers.Add(logger);
            return this;
        }

        public DeviceSessionBuilder WithLoggers(IEnumerable<IPacketLogger> loggers)
        {
            _loggers.AddRange(loggers);
            return this;
        }

        public DeviceSessionBuilder WithPolling(bool autoPolling = true, int interval = 15)
        {
            _isAutoPollingEnabled = autoPolling;
            _pollingInterval = interval;
            return this;
        }

        public DeviceSession Build()
        {
            if (_device == null)
                throw new InvalidOperationException("Device is not configured");
            if (_protocol == null)
                throw new InvalidOperationException("Protocol is not configured");
            return new DeviceSession(_device, _protocol, _loggers, _isAutoPollingEnabled, _pollingInterval);
        }
    }
}
