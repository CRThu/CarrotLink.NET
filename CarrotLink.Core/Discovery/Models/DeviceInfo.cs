using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Discovery.Models
{
    public enum DeviceType
    {
        SerialPort,
        Gpib,
        Ft2232
    }

    /// <summary>
    /// 设备信息
    /// </summary>
    public class DeviceInfo
    {
        public string Interface { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
