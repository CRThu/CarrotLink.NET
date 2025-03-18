
using CarrotLink.Core.Discovery.Models;

namespace CarrotLink.Core.Discovery.Interfaces
{
    public interface IDeviceSearcherFactory
    {
        IDeviceSearcher GetSearcher(DeviceType type);
    }
}