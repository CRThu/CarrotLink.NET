using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarrotLink.Core.Discovery.Models;

namespace CarrotLink.Core.Discovery.Interfaces
{
    public interface IDeviceSearcher
    {
        /// <summary>
        /// 支持的设备类型
        /// </summary>
        DeviceType SupportedType { get; }

        /// <summary>
        /// 搜索可用设备
        /// </summary>
        IEnumerable<DeviceInfo> Search();
    }
}
