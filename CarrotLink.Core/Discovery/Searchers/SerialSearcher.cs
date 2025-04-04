﻿using System;
using System.Collections.Generic;
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
        public DeviceType SupportedType => DeviceType.SerialPort;


        public SerialSearcher()
        {
        }


        public IEnumerable<DeviceInfo> Search()
        {
            return SerialPort.GetPortNames().Select(d => new DeviceInfo(/*"COM", d, "串口设备"*/)).ToArray();
            //return
            //[
            //    new DeviceInfo("COM","200","SERIALPORT COM200 FOR TEST"),
            //    new DeviceInfo("COM","201","SERIALPORT COM201 FOR TEST")
            //];
        }
    }
}
