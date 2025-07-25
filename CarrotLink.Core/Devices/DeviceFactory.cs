using CarrotLink.Core.Devices.Configuration;
using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Devices.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices
{
    public enum InterfaceType
    {
        Ftdi,
        Serial,
        Loopback,
        NiVisa
    }

    public static class DeviceFactory
    {
        public static IDevice Create(InterfaceType type, DeviceConfigurationBase config)
        {
            return type switch
            {
                InterfaceType.Ftdi when config is FtdiConfiguration ftdiConfig =>
                    new FtdiDevice(ftdiConfig),
                InterfaceType.Serial when config is SerialConfiguration serialConfig =>
                    new SerialDevice(serialConfig),
                InterfaceType.Loopback when config is LoopbackConfiguration loopbackConfig =>
                    new LoopbackDevice(loopbackConfig),
                InterfaceType.NiVisa when config is NiVisaConfiguration niVisaConfig =>
                new NiVisaDevice(niVisaConfig),
                _ => throw new ArgumentException($"Invalid device type and configuration combination: {type} with {config.GetType().Name}")
            };
        }
    }
}
