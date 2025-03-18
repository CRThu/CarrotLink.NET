﻿using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;
using CarrotLink.Core.Discovery.Searchers;

namespace CarrotLink.Core.Discovery
{
    public class DeviceSearcherFactory : IDeviceSearcherFactory
    {
        private readonly Dictionary<DeviceType, IDeviceSearcher> _searchers;

        public DeviceSearcherFactory()
        {
            _searchers = new Dictionary<DeviceType, IDeviceSearcher> {
                [DeviceType.SerialPort] = new SerialSearcher(),
                [DeviceType.Gpib] = new NiVisaSearcher(),
                [DeviceType.Ft2232] = new FtdiSearcher()
            };
        }

        public IDeviceSearcher GetSearcher(DeviceType type)
        {
            if (_searchers.TryGetValue(type, out var searcher))
                return searcher;

            throw new NotSupportedException($"Unsupported device type: {type}");
        }
    }
}