using SimpleNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace NetworkingTest
{
    static class TestDefaults
    {
        static GlobalDefaults.EncodingType enc = GlobalDefaults.EncodingType.MESSAGE_PACK;
        static bool OneThread = true;
        static bool UseEncryption = true;

        public delegate bool Condition();
        public delegate Client ClientInitialization();

        public static void SetGlobalDefaults()
        {
            GlobalDefaults.ObjectEncodingType = enc;
            GlobalDefaults.RunServerClientsOnOneThread = OneThread;
            GlobalDefaults.UseEncryption = UseEncryption;
        }

        public static Server GetServer(int clientCap = 1)
        {
            return new Server(IPAddress.Loopback, 9090, (ushort)clientCap);
        }

        public static Client[] GetClientList(ClientInitialization C, int Count)
        {
            Client[] clients = new Client[Count];

            for (int i = 0; i < Count; i++)
            {
                clients[i] = C();
            }

            return clients;
        }


        public static bool WaitForCondition(Condition c, int maxMS = 3000)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            bool b = false;
            while (!(b = c()) && watch.ElapsedMilliseconds < maxMS) ;
            return b;
        }
        public static bool TimeOut(Action a, int maxMS = 3000)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var tsk = Task.Run(a);
            //tsk.Is
            while (!tsk.IsCompleted && watch.ElapsedMilliseconds < maxMS) ;
            return tsk.IsCompleted;
        }

    }
}
