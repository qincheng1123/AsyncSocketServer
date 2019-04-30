using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketServer.Client
{
    class Program
    {
        static Stack<Socket> connectionPool = new Stack<Socket>();

        static int sentCount;

        static void Main(string[] args)
        {
            for (int i = 0; i < 1000; i++)
            {
                //Thread thread = new Thread(new ThreadStart(Send));
                //thread.Start();
                ThreadPool.QueueUserWorkItem(new WaitCallback(Send));
            }

            Console.ReadLine();
        }

        private static void Send(object state)
        {

            while (true)
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                socket.Connect(new IPEndPoint(IPAddress.Parse("192.168.254.130"), 8081));

                socket.Send(System.Text.Encoding.Default.GetBytes("abcdefghijklmnop"));
                byte[] fu = new byte[32];
                int remainingLength = 32;
                while (remainingLength > 0)
                {
                    byte[] buffer = new byte[100];
                    int receCount = socket.Receive(buffer);
                    Buffer.BlockCopy(buffer, 0, fu, fu.Length - remainingLength, receCount);
                    remainingLength -= receCount;
                }
                Interlocked.Increment(ref sentCount);

                Console.WriteLine(System.Text.Encoding.Default.GetString(fu) + "---" + sentCount);
                socket.Shutdown(SocketShutdown.Both);
            }
        }
    }
}
