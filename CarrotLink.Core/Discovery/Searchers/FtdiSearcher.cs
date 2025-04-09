using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FTD2XX_NET.FTDI;
using CarrotLink.Core.Discovery.Models;
using CarrotLink.Core.Discovery.Interfaces;
using CarrotLink.Core.Devices.Library;

namespace CarrotLink.Core.Discovery.Searchers
{
    public class FtdiSearcher : IDeviceSearcher
    {
        public DeviceType SupportedType => DeviceType.Ft2232;
        public FtdiSearcher()
        {
        }

        public IEnumerable<DeviceInfo> Search()
        {
            uint ftdiDeviceCount = 0;

            FTDI ftdi = new FTDI();
            Debug.WriteLine(Path.GetDirectoryName(GetType().Assembly.Location));


            // Determine the number of FTDI devices connected to the machine
            try
            {
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.GetNumberOfDevices(ref ftdiDeviceCount), 100);

                // Allocate storage for device info list
                FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FT_DEVICE_INFO_NODE[ftdiDeviceCount];

                // Populate our device list
                Ftd2xxNetDecorator.Ftd2xxNetWrapper(() => ftdi.GetDeviceList(ftdiDeviceList), 1000);

                /*
                    Device Index: 0
                    Flags: 2
                    Type: FT_DEVICE_2232H
                    ID: 4036010
                    Location ID: 151
                    Serial Number: FT87WWZ2A
                    Description: FT2232H device A

                    Device Index: 1
                    Flags: 2
                    Type: FT_DEVICE_2232H
                    ID: 4036010
                    Location ID: 152
                    Serial Number: FT87WWZ2B
                    Description: FT2232H device B
                */
#if DEBUG
                for (uint i = 0; i < ftdiDeviceCount; i++)
                {
                    Console.WriteLine("Device Index: " + i.ToString());
                    Console.WriteLine("Flags: " + string.Format("{0:x}", ftdiDeviceList[i].Flags));
                    Console.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
                    Console.WriteLine("ID: " + string.Format("{0:x}", ftdiDeviceList[i].ID));
                    Console.WriteLine("Location ID: " + string.Format("{0:x}", ftdiDeviceList[i].LocId));
                    Console.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
                    Console.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
                    Console.WriteLine("");
                }
#endif
                return ftdiDeviceList.Select(dev => new DeviceInfo() {
                    Interface = "FTDI",
                    Name = dev.SerialNumber,
                    Description = dev.Description
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
