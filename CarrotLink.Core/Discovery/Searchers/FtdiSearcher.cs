using FTD2XX_NET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using static FTD2XX_NET.FTDI;
using static CarrotLink.Core.Discovery.Searchers.Ftd2xxNetDecorator;
using CarrotLink.Core.Utility;
using CarrotLink.Core.Discovery.Models;
using CarrotLink.Core.Discovery.Interfaces;

namespace CarrotLink.Core.Discovery.Searchers
{
    public class Ftd2xxException : Exception
    {
        public Ftd2xxException(FT_STATUS ftStatus) : base(ftStatus.ToString()) { }
        public Ftd2xxException(string ftStatus, Exception innerException) : base(ftStatus.ToString(), innerException) { }
    }

    public static class Ftd2xxNetDecorator
    {
        public static void Ftd2xxNetWrapper(Func<FT_STATUS> func, int timeout = 1000)
        {
            try
            {
                FT_STATUS ftStatus = TimeoutDecorator.TimeoutWrapper(func, timeout);
                if (ftStatus != FT_STATUS.FT_OK)
                {
                    throw new Ftd2xxException(ftStatus);
                }
            }
            catch (Exception ex)
            {
                throw new Ftd2xxException("Ftd2xxNetWrapper", ex);
            }
        }

    }

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
                Ftd2xxNetWrapper(() => ftdi.GetNumberOfDevices(ref ftdiDeviceCount), 100);

                // Allocate storage for device info list
                FT_DEVICE_INFO_NODE[] ftdiDeviceList = new FT_DEVICE_INFO_NODE[ftdiDeviceCount];

                // Populate our device list
                Ftd2xxNetWrapper(() => ftdi.GetDeviceList(ftdiDeviceList), 100);

                for (uint i = 0; i < ftdiDeviceCount; i++)
                {
                    Debug.WriteLine("Device Index: " + i.ToString());
                    Debug.WriteLine("Flags: " + string.Format("{0:x}", ftdiDeviceList[i].Flags));
                    Debug.WriteLine("Type: " + ftdiDeviceList[i].Type.ToString());
                    Debug.WriteLine("ID: " + string.Format("{0:x}", ftdiDeviceList[i].ID));
                    Debug.WriteLine("Location ID: " + string.Format("{0:x}", ftdiDeviceList[i].LocId));
                    Debug.WriteLine("Serial Number: " + ftdiDeviceList[i].SerialNumber.ToString());
                    Debug.WriteLine("Description: " + ftdiDeviceList[i].Description.ToString());
                    Debug.WriteLine("");
                }
                return ftdiDeviceList.Select(dev => new DeviceInfo(/*"FTDI", dev.SerialNumber, dev.Description*/))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return [];
            }
        }
    }
}
