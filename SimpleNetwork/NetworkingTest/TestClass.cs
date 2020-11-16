using System;
using Xunit;
using SimpleNetwork;
using System.Net;
using System.Threading;
using System.Diagnostics;

namespace NetworkingTest
{
    public class TestClass
    {
        bool ClientDisconnectInvoked = false;
        bool ServerDisconnectedClient = false;

        int clientsDown = 0;
        int OnConnectCalled = 0;

        int SleepTime = 4000;

        GlobalDefaults.EncodingType enc = GlobalDefaults.EncodingType.MESSAGE_PACK;

        [Fact]
        public void ClientConnectsToServer()
        {
            SetGlobalDefaults();

            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 1);
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, PortGet(0));

            while (s.ClientCount < 1) if (S.ElapsedMilliseconds >= SleepTime) break;

            try
            {
                Assert.True(s.ClientCount == 1);
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Fact]
        public void ClientDisconnectsFromServer()
        {
            SetGlobalDefaults();
            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 1);
            s.StartServer();
            s.OnClientDisconnect += S_OnClientDisconnect;
            s.UpdateWaitTime = 0;

            Client c = new Client();
            c.Connect(IPAddress.Loopback, PortGet(0));
            //c.UpdateWaitTime = 0;

            c.Disconnect();
            while (!ClientDisconnectInvoked) if (S.ElapsedMilliseconds >= SleepTime) break;

            try
            {
                Assert.True(ClientDisconnectInvoked);
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Theory]
        [InlineData(4)]
        void ServerDisconnectsMultipleClients(byte clients)
        {
            SetGlobalDefaults();

            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), clients);
            s.StartServer();

            Client[] Clients = new Client[clients];
            for (int i = 0; i < clients; i++)
            {
                Clients[i] = new Client();
                Clients[i].Connect(IPAddress.Loopback, PortGet(0));
                Clients[i].OnDisconnect += TestClass_OnDisconnect;
                Clients[i].UpdateWaitTime = 0;
            }
            s.DisconnectAllClients();
            while (clientsDown < clients) if (S.ElapsedMilliseconds >= SleepTime) break;

            try
            {
                Assert.Equal(clients, clientsDown);
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Theory]
        [InlineData(456.2346)]
        [InlineData(255)]
        //[InlineData("PEEN")]
        void ClientRecievesObjects<T>(T obj)
        {
            SetGlobalDefaults();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 1);
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, PortGet(0));
            c.UpdateWaitTime = 0;

            s.SendToAll(obj);

            T objec = c.WaitForPullObject<T>();

            try
            {
                Assert.Equal(obj, objec);
            }
            finally
            {
                s.Close();
                c?.Clear();
                Wait();
            }
        }

        [Theory]
        [InlineData(55, 9.999e9)]
        void ServerRecievesObjectsFromClients<T1, T2>(T1 Test1, T2 Test2)
        {
            SetGlobalDefaults();

            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 2);
            s.StartServer();

            Client c1 = new Client();
            c1.Connect(IPAddress.Loopback, PortGet(0));

            Client c2 = new Client();
            c2.Connect(IPAddress.Loopback, PortGet(0));

            c1.SendObject<T1>(Test1);
            c2.SendObject<T2>(Test2);

            while (!s.ClientHasObjectType<T1>(0) || !s.ClientHasObjectType<T2>(1)) if (S.ElapsedMilliseconds >= SleepTime) break;

            T1 o1 = s.PullFromClient<T1>(0);
            T2 o2 = s.PullFromClient<T2>(1);
            try
            {
                Assert.True(Test1.Equals(o1) && Test2.Equals(o2));
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Fact]
        void ClientKeepsDataAfterDisconnect()
        {
            SetGlobalDefaults();

            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 1);
            s.StartServer();

            Client c1 = new Client();
            c1.Connect(IPAddress.Loopback, PortGet(0));

            s.SendToAll("GIN");

            while (!c1.HasObjectType<string>()) if (S.ElapsedMilliseconds >= SleepTime) break;

            c1.Disconnect();

            try
            {
                Assert.Equal("GIN", c1.PullObject<object>());
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Fact]
        void ServerHoldsToClientCap()
        {
            SetGlobalDefaults();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 2);
            s.StartServer();

            Client c1 = new Client();
            c1.BeginConnect(IPAddress.Loopback, PortGet(0));

            Client c2 = new Client();
            c2.BeginConnect(IPAddress.Loopback, PortGet(0));

            Client c3 = new Client();
            c3.BeginConnect(IPAddress.Loopback, PortGet(0));

            Thread.Sleep(SleepTime);

            try
            {
                Assert.Equal(2, s.ClientCount);
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Fact]
        void ServerRestartsOnDisconnect()
        {
            SetGlobalDefaults();

            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 2);
            s.StartServer();
            s.RestartAutomatically = true;
            s.OnClientDisconnect += TestClass_OnDisconnect;
            s.UpdateClientWaitTime = 100;

            Client c1 = new Client();
            //c1.OnConnect += C1_OnConnect;
            c1.UpdateWaitTime = 100;
            c1.Connect(IPAddress.Loopback, PortGet(0));

            Client c2 = new Client();
            c2.BeginConnect(IPAddress.Loopback, PortGet(0));

            Client c3 = new Client();
            c3.BeginConnect(IPAddress.Loopback, PortGet(0));

            c1.Disconnect(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.REMOVE });
            while (clientsDown == 0) if (S.ElapsedMilliseconds >= SleepTime) break;

            try
            {
                Assert.True(clientsDown == 1 && s.ClientCount == 2);
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        [Fact]
        void ClientRecievesMultipleObjectTypes()
        {
            SetGlobalDefaults();

            Stopwatch S = new Stopwatch();
            S.Start();

            Server s = new Server(IPAddress.Loopback, PortGet(0), 1);
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, PortGet(0));

            Exception ex = new Exception(" S DAFADFASD");
            decimal d = 64.463452m;
            string str = "SFAEF";

            s.SendToAll(ex);
            s.SendToAll(d);
            s.SendToAll(str);

            Exception ex1 = null;
            decimal d1 = default;
            string str1 = null;

            new Thread(() =>
            {
                ex1 = c.WaitForPullObject<Exception>();
                d1 = c.WaitForPullObject<decimal>();
                str1 = c.WaitForPullObject<string>();
            }).Start();

            while (ex1 == null || d1 == default || str1 == null) if (S.ElapsedMilliseconds >= SleepTime) break;

            bool b1 = ex.Message == ex1.Message && ex.HResult == ex1.HResult;
            bool b2 = d.Equals(d1);
            bool b3 = str.Equals(str1);
            try
            {
                Assert.True(b1 && b2 && b3);
            }
            finally
            {
                s.Close();
                Wait();
            }
        }

        private void C1_OnConnect(ConnectionInfo inf)
        {
            OnConnectCalled++;
        }

        private void TestClass_OnDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            clientsDown++;
        }
        private void C_OnDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            ServerDisconnectedClient = true;
        }
        private void S_OnClientDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            ClientDisconnectInvoked = true;
        }

        private int PortGet(int id)
        {
            return id + 6669;
        }

        private void SetGlobalDefaults()
        {
            GlobalDefaults.ObjectEncodingType = enc;
        }
        private void Wait()
        {
            Thread.Sleep(100);
        }
    }
}
