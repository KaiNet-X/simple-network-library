using SimpleNetwork;
using System;
using System.Net;
using System.Threading;

namespace NetworkTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Server s = new Server(IPAddress.Loopback, 6669, 1);
            s.StartServer();
            //s.OnClientDisconnect += S_OnClientDisconnect;

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 6669);



            Console.WriteLine(s.ClientCount);

            c.Disconnect();
        }

        private static void S_OnClientDisconnect(DisconnectionContext ctx)
        {
            Console.WriteLine(ctx.type);
        }
    }
}
