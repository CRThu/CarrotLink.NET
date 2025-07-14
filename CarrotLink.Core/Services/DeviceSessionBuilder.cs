using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Logging;
using CarrotLink.Core.Protocols.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services
{
    public class DeviceSessionBuilder
    {
        private IDevice _device;
        private IProtocol _protocol;
        private List<IPacketLogger> _loggers = new List<IPacketLogger>();
        private bool hasProcTask = true;
        private bool hasPollTask = true;
        private int pollInterval;

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

        public DeviceSessionBuilder WithPollTask(bool autoPoll = true, int interval = 15)
        {
            hasPollTask = autoPoll;
            pollInterval = interval;
            return this;
        }

        public DeviceSessionBuilder WithProcessTask(bool autoProc = true)
        {
            hasProcTask = autoProc;
            return this;
        }

        public DeviceSession Build()
        {
            if (_device == null)
                throw new InvalidOperationException("Device is not configured");
            if (_protocol == null)
                throw new InvalidOperationException("Protocol is not configured");
            return new DeviceSession(_device, _protocol, _loggers, hasProcTask, hasPollTask, pollInterval);
        }
    }
}
