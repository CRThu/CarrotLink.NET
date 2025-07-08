using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;

namespace CarrotLink.Core.Discovery.Searchers
{
    public class SerialSearcher : IDeviceSearcher
    {
        public DeviceType SupportedType => DeviceType.Serial;


        public SerialSearcher()
        {
        }


        public IEnumerable<DeviceInfo> Search()
        {
            try
            {
                return SerialPort.GetPortNames().Select(portName => new DeviceInfo()
                {
                    Type = DeviceType.Serial /*"SerialPort"*/,
                    Name = portName,
                    Description = "串口设备"
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return Array.Empty<DeviceInfo>();
            }
        }
    }
}
