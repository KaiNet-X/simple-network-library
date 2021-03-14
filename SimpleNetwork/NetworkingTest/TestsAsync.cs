using SimpleNetwork;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NetworkingTest
{
    public class TestsAsync
    {
        int Disconnections = 0;
        int Connections = 0;
        int files = 0;

        [Fact]
        public async Task ClientConnectsAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            await c.ConnectAsync(IPAddress.Loopback, 9090);

            TestDefaults.WaitForCondition(() => S.ClientCount == 1);

            Assert.True(S.ClientCount == 1);

            S.Close();
        }

        [Theory]
        [InlineData(3, "hello world")]
        [InlineData(3, 99.56)]
        public async void ServerRecievesObjectsAsync<T>(int clients, T obj)
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer(clients);
            S.StartServer();

            Client[] Clients = TestDefaults.GetClientList(() =>
            {
                Client c = new Client();
                c.Connect(IPAddress.Loopback, 9090);
                return c;
            }, clients);

            List<Task> tasks = new List<Task>();
            foreach (Client c in Clients)
                tasks.Add(c.SendObjectAsync(obj));

            foreach (Task t in tasks)
                await t;

            List<Task<T>> tasks2 = new List<Task<T>>();

            for (int i = 0; i < clients; i++)
            {
                tasks.Add(S.PullFromClientAsync<T>((ushort)i));
            }
            Stopwatch sw = new Stopwatch();
            sw.Start();
            bool b = true;
            foreach (Task<T> t in tasks2)
            {
                while (sw.ElapsedMilliseconds < 3000 && !t.IsCompleted) ;
                if (!t.Result.Equals(obj)) b = false;
            }

            Assert.True(b);

            S.Close();
        }

        [Fact]
        public async Task ClientRecievesObjectsAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            Exception e = new Exception();
            bool b = true;
            string s = "AFagea";

            S.SendToAll(e);
            S.SendToAll(b);
            S.SendToAll(s);

            Exception ex = await c.PullObjectAsync<Exception>();
            bool bx = await c.PullObjectAsync<bool>();
            string sx = await c.PullObjectAsync<string>();

            Assert.True(sx == s && b == bx);

            S.Close();
        }

        [Fact]
        public async Task ClientRetrievesQueueAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            Exception e = new Exception();
            bool b = true;
            string s = "AFagea";

            S.SendToAll(e);
            S.SendToAll(b);
            S.SendToAll(s);
            Thread.Sleep(500);

            object[] queue = await c.GetQueueObjectsTypelessAsync(true);

            bool bo = true;

            if (!queue[0].GetType().Equals(e.GetType())) bo = false;
            if (!queue[1].Equals(b)) bo = false;
            if (!queue[2].Equals(s)) bo = false;

            Assert.True(bo);

            S.Close();
        }

        [Fact]
        public async Task ServerRecievesQueuesAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server s = TestDefaults.GetServer(2);
            s.StartServer();

            Client c1 = new Client();
            c1.Connect(IPAddress.Loopback, 9090);

            Client c2 = new Client();
            c2.Connect(IPAddress.Loopback, 9090);

            decimal d = 668.7m;
            string st = "ADGAERTYGAsfjp0ajt0qejtg0ajfgopa8484698";
            string s2 = "F F   F F f";

            await s.SendToAllAsync(d);
            await s.SendToAllAsync(st);
            await s.SendToAllAsync(s2);

            Thread.Sleep(500);

            object[] q1 = await c1.GetQueueObjectsTypelessAsync();
            object[] q2 = await c2.GetQueueObjectsTypelessAsync();

            Assert.True(q1.Length == q2.Length);
        }

        [Fact]
        public async Task ClientSendsFileAsync()
        {
            TestDefaults.SetGlobalDefaults();

            Server s = TestDefaults.GetServer();
            s.StartServer();

            Client c = new Client();
            c.OnFileRecieve += C_OnFileRecieve;
            c.Connect(IPAddress.Loopback, 9090);

            GlobalDefaults.ClearSentFiles();

            string path = Directory.GetCurrentDirectory();
            string f = Directory.GetFiles(path)[0];
            await s.SendFileToAllAsync(f);
        }

        private void C_OnFileRecieve(string path)
        {
            files++;
        }

        private void OnConnect(ConnectionInfo inf)
        {
            Connections++;
        }

        private void OnDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            Disconnections++;
        }
    }
}
