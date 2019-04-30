using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Core
{
    /// <summary>
    /// SocketAsyncEventArgsPool
    /// </summary>
    class SocketAsyncEventArgsPool : IDisposable
    {
        #region 字段
        /// <summary>
        /// 对象池
        /// </summary>
        private Stack<SocketAsyncEventArgs> stack;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="poolSize"></param>
        public SocketAsyncEventArgsPool(int poolSize)
        {
            stack = new Stack<SocketAsyncEventArgs>(poolSize);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 入栈
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        public void Push(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            lock (this.stack)
            {
                this.stack.Push(socketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 出栈
        /// </summary>
        /// <returns></returns>
        public SocketAsyncEventArgs Pop()
        {
            lock (this.stack)
            {
                if (this.stack.Count == 0)
                {
                    return null;
                }
                return stack.Pop();
            }
        }
        /// <summary>
        /// 清理对象
        /// </summary>
        public void Dispose()
        {
            if (this.stack != null)
            {
                foreach (SocketAsyncEventArgs socketAsyncEventArgs in this.stack)
                {
                    if (socketAsyncEventArgs != null)
                        socketAsyncEventArgs.Dispose();
                }
            }
        }
        #endregion
    }
}
