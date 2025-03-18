using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Ports;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Devices
{
    public class SerialDevice : StreamBase
    {
        /// <summary>
        /// 流指示有数据
        /// </summary>
        public override bool ReadAvailable => Driver.BytesToRead > 0;

        /// <summary>
        /// 驱动层实现
        /// </summary>
        private SerialPort Driver { get; set; } = new();

        private readonly SerialConfiguration _config;

        public SerialDevice(SerialConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// 关闭流
        /// </summary>
        public override void Close()
        {
            Driver.Close();
        }

        /// <summary>
        /// 打开流
        /// </summary>
        public override void Open()
        {
            Driver.Open();
        }

        /// <summary>
        /// 流写入
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            Driver.BaseStream.Write(buffer, offset, count);
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
            // TODO 同步流读取存在阻塞，待优化
            return Driver.BaseStream.Read(buffer, offset, count);
        }
    }
}
