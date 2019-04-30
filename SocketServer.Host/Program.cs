using SocketServer.Core;
using System.Net;

namespace SocketServer.Host
{
    class Program
    {
        static void Main(string[] args)
        {
            SocketServerParameter parameter = new SocketServerParameter();
            parameter.AccptPoolSize = 100;
            parameter.Backlog = 100;
            parameter.EndPoint = new System.Net.IPEndPoint(IPAddress.Any, 8008);
            parameter.MaxConnectionCount = 10000;
            parameter.ReceiveMessageLength = 16;
            parameter.SendMessageLength = 32;
            parameter.SingletonBufferSize = 32;
            Core.SocketServer server = new Core.SocketServer(parameter);
            server.Start();

            System.Console.ReadLine();
        }
    }
}
