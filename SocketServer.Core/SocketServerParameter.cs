using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Core
{
    /// <summary>
    /// SocketServerParameter
    /// </summary>
    public class SocketServerParameter
    {
        /// <summary>
        /// 接收请求对象池Size
        /// </summary>
        public int AccptPoolSize { get; set; }
        /// <summary>
        /// 最大连接请求挂起数量
        /// </summary>
        public int Backlog { get; set; }
        /// <summary>
        /// 监听地址端口
        /// </summary>
        public IPEndPoint EndPoint { get; set; }
        /// <summary>
        /// 最大同时连接数
        /// </summary>
        public int MaxConnectionCount { get; set; }
        /// <summary>
        /// 单个缓冲区大小
        /// </summary>
        public int SingletonBufferSize { get; set; }
        /// <summary>
        /// 接收消息长度
        /// </summary>
        public int ReceiveMessageLength { get; set; }
        /// <summary>
        /// 发送消息长度
        /// </summary>
        public int SendMessageLength { get; set; }
    }
}
