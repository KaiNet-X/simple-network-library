using Newtonsoft.Json;
using SimpleNetwork;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Windows.Forms;

namespace NetworkTester
{
    class Program
    {
        static Server S = new Server(IPAddress.Loopback, 8888, 2);

        static void Main(string[] args)
        {
            S.OnClientRecieveFile += S_OnClientRecieveFile;
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 8888);
            string f = @"C:\Users\Kai\Desktop\MassPrint.PNG";
            c.SendFile(f, "NAM");
            Console.ReadKey();
        }

        private static void S_OnClientRecieveFile(SimpleFile file, ConnectionInfo info)
        {
            string Path = Directory.GetCurrentDirectory();
            Console.WriteLine(file.MoveToPath(Path, "test", true).Name);
        }
    }
}
