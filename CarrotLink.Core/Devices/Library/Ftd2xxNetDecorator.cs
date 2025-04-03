using CarrotLink.Core.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FTD2XX_NET.FTDI;

namespace CarrotLink.Core.Devices.Library
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

}
