using CarrotLink.Core.Devices;
using CarrotLink.Core.Protocols;
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

        /// <summary>
        /// 支持协议列表
        /// </summary>
        public ProtocolType[]? SupportProtocols { get; init; }
        
        /// <summary>
        /// 是否支持定时自动轮询接收数据(例如VISA设备不支持)
        /// </summary>
        public bool SupportAutoPolling { get; init; } = true;
    }
}
