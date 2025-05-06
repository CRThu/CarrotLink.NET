using CarrotLink.Core.Devices.Interfaces;
using CarrotLink.Core.Logging;
using CarrotLink.Core.Protocols.Models;
using CarrotLink.Core.Storage;
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
        private IDataStorage _storage;
        private List<ILogger> _loggers = new List<ILogger>();

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

        public DeviceServiceBuilder WithStorage(IDataStorage storage)
        {
            _storage = storage;
            return this;
        }

        public DeviceServiceBuilder WithLogger(ILogger logger)
        {
            _loggers.Add(logger);
            return this;
        }

        public DeviceServiceBuilder WithLoggers(IEnumerable<ILogger> loggers)
        {
            _loggers.AddRange(loggers);
            return this;
        }

        public DeviceService Build()
        {
            return new DeviceService(_device, _protocol, _storage, _loggers);
        }
    }
}
