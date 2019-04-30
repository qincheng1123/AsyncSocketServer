using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Core
{
    /// <summary>
    /// BufferManager
    /// </summary>
    class BufferManager
    {
        #region 字段
        /// <summary>
        /// 数据缓冲区
        /// </summary>
        private byte[] buffer;
        /// <summary>
        /// 最大连接数
        /// </summary>
        private int maxConnectionCount;
        /// <summary>
        /// 单个缓冲区大小
        /// </summary>
        private int singletonBufferSize;
        /// <summary>
        /// 缓冲区偏移量
        /// </summary>
        private int bufferOffset;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="socketServerParameter"></param>
        public BufferManager(SocketServerParameter socketServerParameter)
        {
            this.maxConnectionCount = socketServerParameter.MaxConnectionCount;
            this.singletonBufferSize = socketServerParameter.SingletonBufferSize;
            this.buffer = new byte[maxConnectionCount * singletonBufferSize];
            this.bufferOffset = 0;
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 初始化接收发送数据对象缓冲区
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        public void InitSocketAsyncEventArgsBuffer(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            if (this.bufferOffset + singletonBufferSize > maxConnectionCount * singletonBufferSize)
            {
                throw new Exception("已超出缓冲区最大长度");
            }
            socketAsyncEventArgs.SetBuffer(this.buffer, this.bufferOffset, this.singletonBufferSize);
            this.bufferOffset += this.singletonBufferSize;
        }
        #endregion
    }
}
