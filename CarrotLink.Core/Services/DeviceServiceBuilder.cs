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
    public class DeviceServiceBuilder
    {
        private IDevice _device;
        private IProtocol _protocol;
        private List<IPacketLogger> _loggers = new List<IPacketLogger>();

        public DeviceServiceBuilder WithDevice(IDevice device)
        {
            _device = device;
            return this;
        }

        public DeviceServiceBuilder WithProtocol(IProtocol protocol)
        {
            _protocol = protocol;
            return this;
        }

        public DeviceServiceBuilder WithLogger(IPacketLogger logger)
        {
            _loggers.Add(logger);
            return this;
        }

        public DeviceServiceBuilder WithLoggers(IEnumerable<IPacketLogger> loggers)
        {
            _loggers.AddRange(loggers);
            return this;
        }

        public DeviceService Build()
        {
            return new DeviceService(_device, _protocol, _loggers);
        }
    }
}
