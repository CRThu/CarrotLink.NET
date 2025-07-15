using CarrotLink.Core.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Discovery.Models
{
    public enum DriverType
    {
        Serial,
        NiVisa,
        Ftdi
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public record DeviceInfo
    {
        public DriverType Driver { get; init; }
        public InterfaceType Interface { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
    }
}
