using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer.Core
{
    /// <summary>
    /// SocketServer
    /// </summary>
    public class SocketServer : IDisposable
    {
        #region 字段
        /// <summary>
        /// SocketServer参数
        /// </summary>
        private SocketServerParameter socketServerParameter;
        /// <summary>
        /// 接收请求socket对象
        /// </summary>
        private Socket socket;
        /// <summary>
        /// 接收发送数据缓冲区
        /// </summary>
        private BufferManager bufferManager;
        /// <summary>
        /// 接收请求对象池
        /// </summary>
        private SocketAsyncEventArgsPool acceptPool;
        /// <summary>
        /// 接收发送数据对象池
        /// </summary>
        private SocketAsyncEventArgsPool receiveSendPool;
        /// <summary>
        /// 最大连接数控制单元
        /// </summary>
        private Semaphore semaphore;
        #endregion

        #region 构造方法
        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="socketServerParameter"></param>
        public SocketServer(SocketServerParameter socketServerParameter)
        {
            this.socketServerParameter = socketServerParameter;
            /*
            1、初始化数据缓冲区
            2、初始化接收请求对象池
            3、初始化接收发送数据对象池
            4、初始化最大连接数量控制单元
            5、初始化接收请求Socket对象
             */
            //1.
            this.bufferManager = new BufferManager(socketServerParameter);
            //2.
            this.acceptPool = new SocketAsyncEventArgsPool(socketServerParameter.AccptPoolSize);
            for (int i = 0; i < socketServerParameter.AccptPoolSize; i++)
            {
                this.acceptPool.Push(CreateAcceptSocketAsyncEventArgs());
            }
            //3.
            this.receiveSendPool = new SocketAsyncEventArgsPool(socketServerParameter.MaxConnectionCount);
            for (int i = 0; i < socketServerParameter.MaxConnectionCount; i++)
            {
                this.receiveSendPool.Push(CreateReceiveSendSocketAsyncEventArgs());
            }
            //4.
            this.semaphore = new Semaphore(socketServerParameter.MaxConnectionCount, socketServerParameter.MaxConnectionCount);
            //5.
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(socketServerParameter.EndPoint);
            socket.Listen(socketServerParameter.Backlog);
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 开始服务
        /// </summary>
        public void Start()
        {
            this.StartAccept();
        }
        /// <summary>
        /// 销毁对象
        /// </summary>
        public void Dispose()
        {
            if (this.acceptPool != null)
            {
                this.acceptPool.Dispose();
            }
            if (this.receiveSendPool != null)
            {
                this.receiveSendPool.Dispose();
            }
            if (this.socket != null)
            {
                this.socket.Close();
            }
            if (this.semaphore != null)
            {
                this.semaphore.Dispose();
            }
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 开始接收请求
        /// </summary>
        private void StartAccept()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = this.acceptPool.Pop();
            if (socketAsyncEventArgs == null)
            {
                socketAsyncEventArgs = CreateAcceptSocketAsyncEventArgs();
            }

            this.semaphore.WaitOne();

            if (!this.socket.AcceptAsync(socketAsyncEventArgs))
            {
                ProcessAccept(socketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 处理接收请求
        /// </summary>
        /// <param name="acceptSocketAsyncEventArgs"></param>
        private void ProcessAccept(SocketAsyncEventArgs acceptSocketAsyncEventArgs)
        {
            if (acceptSocketAsyncEventArgs.SocketError != SocketError.Success)
            {
                HandleBadAccept(acceptSocketAsyncEventArgs);
            }
            else
            {
                SocketAsyncEventArgs receiveSocketAsyncEventArgs = this.receiveSendPool.Pop();
                receiveSocketAsyncEventArgs.AcceptSocket = acceptSocketAsyncEventArgs.AcceptSocket;
                acceptSocketAsyncEventArgs.AcceptSocket = null;
                this.acceptPool.Push(acceptSocketAsyncEventArgs);
                StartReceive(receiveSocketAsyncEventArgs);
            }
            StartAccept();
        }
        /// <summary>
        /// 开始接收数据
        /// </summary>
        /// <param name="receiveSocketAsyncEventArgs"></param>
        private void StartReceive(SocketAsyncEventArgs receiveSocketAsyncEventArgs)
        {
            if (!receiveSocketAsyncEventArgs.AcceptSocket.ReceiveAsync(receiveSocketAsyncEventArgs))
            {
                ProcessReceive(receiveSocketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 处理接收数据
        /// </summary>
        /// <param name="receiveSocketAsyncEventArgs"></param>
        private void ProcessReceive(SocketAsyncEventArgs receiveSocketAsyncEventArgs)
        {
            if (receiveSocketAsyncEventArgs.SocketError == SocketError.Success && receiveSocketAsyncEventArgs.BytesTransferred > 0)
            {
                DataHolder dataHolder = receiveSocketAsyncEventArgs.UserToken as DataHolder;

                SocketProcessStatus socketProcessStatus = dataHolder.Receive(receiveSocketAsyncEventArgs);
                switch (socketProcessStatus)
                {
                    case SocketProcessStatus.UnComplete:
                        StartReceive(receiveSocketAsyncEventArgs);
                        break;
                    case SocketProcessStatus.Completed:
                        if (dataHolder.HandleMessage())
                        {
                            //处理数据成功
                            StartSend(receiveSocketAsyncEventArgs);
                        }
                        else
                        {
                            //处理数据异常
                            CloseClientSocket(receiveSocketAsyncEventArgs);
                        }
                        break;
                    case SocketProcessStatus.Exception:
                        CloseClientSocket(receiveSocketAsyncEventArgs);
                        break;
                }
            }
            else
            {
                CloseClientSocket(receiveSocketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 开始发送数据
        /// </summary>
        /// <param name="sendSocketAsyncEventArgs"></param>
        private void StartSend(SocketAsyncEventArgs sendSocketAsyncEventArgs)
        {
            (sendSocketAsyncEventArgs.UserToken as DataHolder).Send(sendSocketAsyncEventArgs);
            if (!sendSocketAsyncEventArgs.AcceptSocket.SendAsync(sendSocketAsyncEventArgs))
            {
                ProcessSend(sendSocketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 处理发送数据
        /// </summary>
        /// <param name="sendSocketAsyncEventArgs"></param>
        private void ProcessSend(SocketAsyncEventArgs sendSocketAsyncEventArgs)
        {
            if (sendSocketAsyncEventArgs.SocketError == SocketError.Success)
            {
                DataHolder dataHolder = sendSocketAsyncEventArgs.UserToken as DataHolder;
                switch (dataHolder.SendStatus)
                {
                    case SocketProcessStatus.UnComplete:
                        StartSend(sendSocketAsyncEventArgs);
                        break;
                    case SocketProcessStatus.Completed:
                        CloseClientSocket(sendSocketAsyncEventArgs);
                        break;
                }
            }
            else
            {
                CloseClientSocket(sendSocketAsyncEventArgs);
            }
        }
        /// <summary>
        /// 接收请求完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Accept_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        /// <summary>
        /// 接收发送数据完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ReceiveSend_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    break;
            }
        }
        /// <summary>
        /// 创建Accept对象
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateAcceptSocketAsyncEventArgs()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            socketAsyncEventArgs.Completed += Accept_Completed;
            return socketAsyncEventArgs;
        }
        /// <summary>
        /// 创建接收发送数据对象
        /// </summary>
        /// <returns></returns>
        private SocketAsyncEventArgs CreateReceiveSendSocketAsyncEventArgs()
        {
            SocketAsyncEventArgs socketAsyncEventArgs = new SocketAsyncEventArgs();
            this.bufferManager.InitSocketAsyncEventArgsBuffer(socketAsyncEventArgs);
            socketAsyncEventArgs.UserToken = new DataHolder(this.socketServerParameter);
            socketAsyncEventArgs.Completed += ReceiveSend_Completed;
            return socketAsyncEventArgs;
        }
        /// <summary>
        /// 关闭客户端Socket
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        private void CloseClientSocket(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            try
            {
                socketAsyncEventArgs.AcceptSocket.Shutdown(SocketShutdown.Both);
            }
            catch { }
            socketAsyncEventArgs.AcceptSocket.Close();
            socketAsyncEventArgs.AcceptSocket = null;
            (socketAsyncEventArgs.UserToken as DataHolder).Reset();
            socketAsyncEventArgs.SetBuffer(socketAsyncEventArgs.Offset, this.socketServerParameter.SingletonBufferSize);
            this.receiveSendPool.Push(socketAsyncEventArgs);
            this.semaphore.Release();
        }
        /// <summary>
        /// 处理接收请求异常
        /// </summary>
        /// <param name="socketAsyncEventArgs"></param>
        private void HandleBadAccept(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            socketAsyncEventArgs.AcceptSocket.Close();
            socketAsyncEventArgs.AcceptSocket = null;
            this.acceptPool.Push(socketAsyncEventArgs);
            this.semaphore.Release();
        }
        #endregion
    }
}
