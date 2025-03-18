using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;

namespace CarrotLink.Core.Discovery
{
    public class DeviceDiscoveryService
    {
        private readonly IDeviceSearcherFactory _factory;

        public DeviceDiscoveryService(IDeviceSearcherFactory factory)
        {
            _factory = factory;
        }

        public IEnumerable<DeviceInfo> DiscoverDevices(DeviceType type)
        {
            var searcher = _factory.GetSearcher(type);
            return searcher.Search();
        }

        public Dictionary<DeviceType, IEnumerable<DeviceInfo>> DiscoverAll()
        {
            return Enum.GetValues(typeof(DeviceType))
                .Cast<DeviceType>()
                .ToDictionary(
                    type => type,
                    type => DiscoverDevices(type)
                );
        }
    }
}