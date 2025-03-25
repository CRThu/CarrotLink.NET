using System;
using CarrotLink.Core.Devices.Impl;
using CarrotLink.Core.Devices.Configuration;
using System.Runtime.InteropServices;

namespace CarrotLink.Native
{
    public static unsafe class NativeExports
    {
        [UnmanagedCallersOnly(EntryPoint = "init_serial_device")]
        public static int InitSerialDevice(
            byte* deviceId, int deviceIdLen,
            byte* portName, int portNameLen,
            int baudRate,
            IntPtr* deviceHandle)
        {
            try
            {
                var config = new SerialConfiguration {
                    DeviceId = Marshal.PtrToStringUTF8((IntPtr)deviceId, deviceIdLen),
                    PortName = Marshal.PtrToStringUTF8((IntPtr)portName, portNameLen),
                    BaudRate = baudRate
                };

                var device = new SerialDevice(config);
                *deviceHandle = GCHandle.ToIntPtr(GCHandle.Alloc(device));
                return 0;
            }
            catch
            {
                *deviceHandle = IntPtr.Zero;
                return -1;
            }
        }
    }
}