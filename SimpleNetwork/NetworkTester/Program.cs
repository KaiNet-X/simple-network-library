using Newtonsoft.Json;
using SimpleNetwork;
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace NetworkTester
{
    class Program
    {
        static Server S = new Server(IPAddress.Loopback, 8888, 2);

        static void Main(string[] args)
        {
            S.StartServer();

            GlobalDefaults.UseEncryption = true;

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 8888);

            S.SendToAll(new tt() { str = "REEEE" });

            object[] attributes = typeof(tt).GetFields()[1].GetCustomAttributes(false);

            tt t = c.WaitForPullObject<tt>();
        }

        private static void S_OnClientConnect(ConnectionInfo inf)
        {
            S.SendToAll("Hello");
            Console.WriteLine("Lock contention free!!");
        }

        public class tt
        {
            public string s = "ffffff";
            public string str = "foo";
        }

        public class L1
        {
            public string name = "yes";
            public int ID = 3;
            public L2 lev2 = new L2();
            public L3[] Lev3;

            public class L2
            {
                string s = "ree";
                public L3 Lev3 = null;
            }
            public class L3
            {
                public string dfaf = "sdfgsdg";
                public int[] ints = { 235, 5235, 23357, 6, 420 };
            }
        }
    }
}
