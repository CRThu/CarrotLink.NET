using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CarrotLink.Core.Protocols.Models
{
    /// <summary>
    /// 数据包
    /// </summary>
    public class PacketBase
    {
        public static PacketBase Empty => new([]);

        /// <summary>
        /// 字节数组
        /// </summary>
        public virtual byte[]? Bytes { get; set; }

        /// <summary>
        /// 数据包可阅读信息
        /// </summary>
        public virtual string? Message => null;
        public virtual byte? ProtocolId => null;
        public virtual byte? StreamId => null;

        public PacketBase()
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PacketBase(byte[] bytes)
        {
            Bytes = bytes;
        }
    }
}
