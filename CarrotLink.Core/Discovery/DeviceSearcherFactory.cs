using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;
using CarrotLink.Core.Discovery.Searchers;

namespace CarrotLink.Core.Discovery
{
    public class DeviceSearcherFactory : IDeviceSearcherFactory
    {
        private readonly Dictionary<DriverType, IDeviceSearcher> _searchers;

        public DeviceSearcherFactory()
        {
            _searchers = new Dictionary<DriverType, IDeviceSearcher> {
                [DriverType.Serial] = new SerialSearcher(),
                [DriverType.NiVisa] = new NiVisaSearcher(),
                [DriverType.Ftdi] = new FtdiSearcher()
            };
        }

        public IDeviceSearcher GetSearcher(DriverType type)
        {
            if (_searchers.TryGetValue(type, out var searcher))
                return searcher;

            throw new NotSupportedException($"Unsupported device type: {type}");
        }
    }
}