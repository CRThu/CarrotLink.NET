using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Logging
{
    /// <summary>
    /// 记录器接口
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 实例唯一名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 记录事件回调委托
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">数据包</param>
        public delegate void LogEventHandler(object sender, LogEventArgs e);

        /// <summary>
        /// 记录器回调方法
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">数据包</param>
        public void Log(object sender, LogEventArgs e);

    }

    public class LogEventArgs : EventArgs
    {
        public DateTime Time { get; set; }
        public string From { get; set; }
        //public Packet? Packet { get; set; }
    }
}
