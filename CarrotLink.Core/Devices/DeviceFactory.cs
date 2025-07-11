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
    public enum DeviceType
    {
        Ftdi,
        Serial,
        Loopback,
        NiVisa
    }

    public static class DeviceFactory
    {
        public static IDevice Create(DeviceType type, DeviceConfigurationBase config)
        {
            return type switch
            {
                DeviceType.Ftdi when config is FtdiConfiguration ftdiConfig =>
                    new FtdiDevice(ftdiConfig),
                DeviceType.Serial when config is SerialConfiguration serialConfig =>
                    new SerialDevice(serialConfig),
                DeviceType.Loopback when config is LoopbackConfiguration loopbackConfig =>
                    new LoopbackDevice(loopbackConfig),
                DeviceType.NiVisa when config is NiVisaConfiguration niVisaConfig =>
                    throw new NotImplementedException("NiVisaDevice is not implemented yet"),
                _ => throw new ArgumentException($"Invalid device type and configuration combination: {type} with {config.GetType().Name}")
            };
        }
    }
}
