using SimpleNetwork;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using Xunit;

namespace NetworkingTest
{
    public class Tests
    {
        int Disconnections = 0;
        int Connections = 0;

        [Fact]
        public void ClientConnects()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            TestDefaults.WaitForCondition(() => S.ClientCount == 1);

            Assert.True(S.ClientCount == 1);

            S.Close();
        }

        [Fact]
        public void ClientDisconnects()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            bool eval = false;

            TestDefaults.WaitForCondition(() => S.ClientCount == 1);

            eval = S.ClientCount == 1;

            c.Disconnect();

            TestDefaults.WaitForCondition(() => !S.ReadonlyClients[0].IsConnected);

            eval = eval && !S.ReadonlyClients[0].IsConnected;

            Assert.True(eval);

            S.Close();
        }

        [Fact]
        public void ClientConnectInvokes()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.OnConnect += OnConnect;
            c.BeginConnect(IPAddress.Loopback, 9090);

            TestDefaults.WaitForCondition(() => Connections == 1);

            Assert.True(Connections == 1);

            S.Close();
        }

        [Fact]
        public void ClientDisconnectInvokes()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.OnDisconnect += OnDisconnect;
            c.BeginConnect(IPAddress.Loopback, 9090);

            TestDefaults.WaitForCondition(() => S.ClientCount == 1, 5000);
            //Thread.Sleep(1000);
            S.DisconnectAllClients();

            TestDefaults.WaitForCondition(() => Disconnections == 1);

            Assert.True(Disconnections == 1);

            S.Close();
        }

        [Fact]
        public void ServerConnectInvokes()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.OnClientConnect += ServerClientConnect;
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            TestDefaults.WaitForCondition(() => Disconnections == 1);

            Assert.True(Connections == 1);

            S.Close();
        }

        [Fact]
        public void ServerDisconnectInvokes()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.OnClientDisconnect += OnDisconnect;
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            TestDefaults.WaitForCondition(() => S.ClientCount == 1);

            c.Disconnect();

            TestDefaults.WaitForCondition(() => Disconnections == 1);

            Assert.True(Disconnections == 1);

            S.Close();
        }

        [Theory]
        [InlineData(3, "hello world")]
        [InlineData(3, 99.56)]
        public void ServerRecievesObjects<T>(int clients, T obj)
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

            foreach (Client c in Clients)
                c.SendObject(obj);

            bool b = TestDefaults.TimeOut(() =>
            {
                for (int i = 0; i < clients; i++)
                {
                    S.WaitForPullFromClient<T>((ushort)i);
                }
            }, 6000);

            Assert.True(b);

            S.Close();
        }

        [Fact]
        public void ClientRecievesObjects()
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

            Exception ex = c.WaitForPullObject<Exception>();
            bool bx = c.WaitForPullObject<bool>();
            string sx = c.WaitForPullObject<string>();

            Assert.True(sx == s && b == bx);

            S.Close();
        }

        [Fact]
        public void ClientRetrievesQueue()
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
            Assert.True(TestDefaults.WaitForCondition(() =>
            {
                object[] queue = c.GetQueueObjectsTypeless(true);

                if (!queue[0].GetType().Equals(e.GetType())) return false;
                if (!queue[1].Equals(b)) return false;
                if (!queue[2].Equals(s)) return false;
                return true;
            }));

            S.Close();
        }

        [Fact]
        public void ServerRecievesQueues()
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

            s.SendToAll(d);
            s.SendToAll(st);
            s.SendToAll(s2);

            Thread.Sleep(500);

            object[] q1 = c1.GetQueueObjectsTypeless();
            object[] q2 = c2.GetQueueObjectsTypeless();

            Assert.True(q1.Length == q2.Length);
        }

        [Fact]
        public void RecievesInherited()
        {
            TestDefaults.SetGlobalDefaults();

            Server S = TestDefaults.GetServer();
            S.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 9090);

            var test = new TestDefaults.TestClass() { ID = 42, Foo = "foos", Str = "String"};

            S.SendToAll(test);
            S.SendToAll(test);
            S.SendToAll(test);
            S.SendToAll(test);

            Assert.True(TestDefaults.TimeOut(() =>
            {
                var v1 = c.WaitForPullObject<TestDefaults.TestClass>();
                var v2 = c.WaitForPullObject<TestDefaults.TestBase>();
                var v3 = c.WaitForPullObject<TestDefaults.TestInterface>();
                var v4 = c.WaitForPullObject<object>();
                while (v1.ID != 42 || v1.GetFoo() != "foos" || v1.st != "String") ;
                while (v2.GetFoo() != "foos" || v2.GetID() != 42) ;
                while (v3.GetVal() != 42 || v3.Str != "String") ;
                while ((v4 as TestDefaults.TestClass).ID != 42) ;
            }, 50000));
        }
        private void ServerClientConnect(ConnectionInfo inf, ushort index)
        {
            Connections++;
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
