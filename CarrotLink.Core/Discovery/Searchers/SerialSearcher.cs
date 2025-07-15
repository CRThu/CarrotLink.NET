using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CarrotLink.Core.Devices;
using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Discovery.Models;

namespace CarrotLink.Core.Discovery.Searchers
{
    public class SerialSearcher : IDeviceSearcher
    {
        public DriverType SupportedType => DriverType.Serial;


        public SerialSearcher()
        {
        }


        public IEnumerable<DeviceInfo> Search()
        {
            try
            {
                return SerialPort.GetPortNames().Select(portName => new DeviceInfo()
                {
                    Driver = DriverType.Serial /*"SerialPort"*/,
                    Interface = InterfaceType.Serial,
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
