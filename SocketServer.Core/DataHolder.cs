using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Core
{
    /// <summary>
    /// DataHolder
    /// </summary>
    class DataHolder
    {
        #region 字段
        /// <summary>
        /// 单独缓冲区大小
        /// </summary>
        private int singleBufferSize;
        /// <summary>
        /// 接收字节总数
        /// </summary>
        private int receiveMessageLength;
        /// <summary>
        /// 发送字节总数
        /// </summary>
        private int sendMessageLength;
        /// <summary>
        /// 已接收字节数
        /// </summary>
        private int receivedLength;
        /// <summary>
        /// 已发送字节数
        /// </summary>
        private int sentLength;
        /// <summary>
        /// 接收字节缓冲区
        /// </summary>
        private byte[] receiveBuffer;
        /// <summary>
        /// 发送字节缓冲区
        /// </summary>
        private byte[] sendBuffer;
        #endregion

        #region 属性
        /// <summary>
        /// 发送数据结果
        /// </summary>
        public SocketProcessStatus SendStatus
        {
            get
            {
                return this.sendMessageLength == this.sentLength ? SocketProcessStatus.Completed : SocketProcessStatus.UnComplete;
            }
        }
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="socketServerParameter"></param>
        public DataHolder(SocketServerParameter socketServerParameter)
        {
            this.receiveMessageLength = socketServerParameter.ReceiveMessageLength;
            this.sendMessageLength = socketServerParameter.SendMessageLength;
            this.singleBufferSize = socketServerParameter.SingletonBufferSize;
            this.receiveBuffer = new byte[receiveMessageLength];
            this.sendBuffer = new byte[sendMessageLength];
            this.Reset();
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 重置
        /// </summary>
        public void Reset()
        {
            this.receivedLength = 0;
            this.sentLength = 0;
        }
        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="receiveSocketAsyncEventArgs"></param>
        /// <returns></returns>
        public SocketProcessStatus Receive(SocketAsyncEventArgs receiveSocketAsyncEventArgs)
        {
            int receiveCount = receiveSocketAsyncEventArgs.BytesTransferred;
            if (this.receivedLength + receiveCount < this.receiveMessageLength)
            {
                //本次未接收完成
                Buffer.BlockCopy(receiveSocketAsyncEventArgs.Buffer, receiveSocketAsyncEventArgs.Offset, this.receiveBuffer, this.receivedLength, receiveCount);
                this.receivedLength += receiveCount;
                return SocketProcessStatus.UnComplete;
            }
            else if (this.receivedLength + receiveCount == this.receiveMessageLength)
            {
                //本次接收完成
                Buffer.BlockCopy(receiveSocketAsyncEventArgs.Buffer, receiveSocketAsyncEventArgs.Offset, this.receiveBuffer, this.receivedLength, receiveCount);
                this.receivedLength += receiveCount;
                return SocketProcessStatus.Completed;
            }
            else
            {
                return SocketProcessStatus.Exception;
            }
        }
        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        /// <returns></returns>
        public bool HandleMessage()
        {
            //接收到数据之后的处理逻辑，需要自定义
            bool result = false;
            byte option = this.receiveBuffer[0];
            switch (option)
            {
                case 97:
                    Buffer.BlockCopy(this.receiveBuffer, 0, this.sendBuffer, 0, this.receivedLength);
                    Buffer.BlockCopy(this.receiveBuffer, 0, this.sendBuffer, 16, this.receivedLength);
                    result = true;
                    break;
                default:
                    break;
            }
            return result;
        }
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="sendSocketAsyncEventArgs"></param>
        public void Send(SocketAsyncEventArgs sendSocketAsyncEventArgs)
        {
            int remainingCount = this.sendMessageLength - this.sentLength;
            if (remainingCount > singleBufferSize)
            {
                //本次无法发送完成
                Buffer.BlockCopy(this.sendBuffer, this.sentLength, sendSocketAsyncEventArgs.Buffer, sendSocketAsyncEventArgs.Offset, singleBufferSize);
                this.sentLength += singleBufferSize;
            }
            else
            {
                //本次将发送完成
                Buffer.BlockCopy(this.sendBuffer, this.sentLength, sendSocketAsyncEventArgs.Buffer, sendSocketAsyncEventArgs.Offset, remainingCount);
                this.sentLength += remainingCount;
                sendSocketAsyncEventArgs.SetBuffer(sendSocketAsyncEventArgs.Offset, remainingCount);
            }
        }
        #endregion
    }
}
