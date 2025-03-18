using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using NationalInstruments.VisaNS;
using System.Diagnostics;
using CarrotLink.Core.Devices.Configuration;

namespace CarrotLink.Core.Devices.Impl
{
    public class NiVisaDevice : StreamBase
    {

        /// <summary>
        /// 流指示有数据
        /// </summary>
        public override bool ReadAvailable
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// 驱动层实现
        /// </summary>
        protected MessageBasedSession Session { get; set; }

        private readonly NiVisaConfiguration _config;

        public NiVisaDevice(NiVisaConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 关闭流
        /// </summary>
        public override void Close()
        {
        }

        /// <summary>
        /// 打开流
        /// </summary>
        public override void Open()
        {
            var res = (MessageBasedSession)ResourceManager.GetLocalManager().Open("ADDR");
            if (res is MessageBasedSession)
                Session = res;
            else
                throw new Exception();

            Session.Timeout = 30000;
        }

        /// <summary>
        /// 流写入
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] bytes = new byte[count];
            Array.Copy(buffer, offset, bytes, 0, count);
            Session.Write(buffer);
        }

        /// <summary>
        /// 流读取
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                byte[] x = Session.ReadByteArray(count);
                Array.Copy(x, 0, buffer, offset, x.Length);
                return x.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}
