using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Services.Logging
{
    /// <summary>
    /// 记录器基础类
    /// </summary>
    public class LoggerBase : ILogger
    {
        /// <summary>
        /// 实例唯一名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LoggerBase()
        {
            Name = "__NAME__";
        }

        /// <summary>
        /// 记录器回调方法
        /// </summary>
        /// <param name="sender">发送者</param>
        /// <param name="e">数据包</param>
        public virtual void Log(object sender, LogEventArgs e)
        {
            //Debug.WriteLine($"{GetType().FullName}: " + e.Packet.Message);
        }
    }
}
