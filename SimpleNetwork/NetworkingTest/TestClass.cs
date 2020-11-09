using System;
using Xunit;
using SimpleNetwork;
using System.Net;
using System.Threading;

namespace NetworkingTest
{
    public class TestClass
    {
        bool ClientDisconnectInvoked = false;
        bool ServerDisconnectedClient = false;

        int clientsDown = 0;
        int OnConnectCalled = 0;

        int SleepTime = 4000;

        [Fact]
        public void ClientConnectsToServer()
        {
            Server s = new Server(IPAddress.Loopback, 6669, 1);
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 6669);

            Thread.Sleep(SleepTime);

            Assert.True(s.ClientCount == 1);
        }

        [Fact]
        public void ClientDisconnectsFromServer()
        {
            Server s = new Server(IPAddress.Loopback, 6666, 1);
            s.StartServer();
            s.OnClientDisconnect += S_OnClientDisconnect;
            s.UpdateWaitTime = 0;

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 6666);
            //c.UpdateWaitTime = 0;

            c.Disconnect();
            Thread.Sleep(SleepTime);

            Assert.True(ClientDisconnectInvoked);
        }

        [Fact]
        public void ServerDisconnectsClient()
        {
            Server s = new Server(IPAddress.Loopback, 6667, 1);
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 6667);
            c.OnDisconnect += C_OnDisconnect;

            Thread.Sleep(SleepTime);

            s.DisconnectAllClients();

            Thread.Sleep(SleepTime);

            Assert.True(ServerDisconnectedClient);
        }

        [Theory]
        [InlineData(5)]
        void ServerDisconnectsMultipleClients(byte clients)
        {
            Server s = new Server(IPAddress.Loopback, 6668, clients);
            s.StartServer();

            Client[] Clients = new Client[clients];
            for (int i = 0; i < clients; i++)
            {
                Clients[i] = new Client();
                Clients[i].Connect(IPAddress.Loopback, 6668);
                Clients[i].OnDisconnect += TestClass_OnDisconnect;
            }
            s.DisconnectAllClients();
            Thread.Sleep(SleepTime);

            Assert.True(clientsDown == clients);
        }

        [Theory]
        [InlineData(456.2346)]
        [InlineData(6969)]
        //[InlineData("PEEN")]
        void ClientRecievesObjects<T>(T obj)
        {
            Server s = new Server(IPAddress.Loopback, 6669, 1);
            s.StartServer();

            Client c = new Client();
            c.Connect(IPAddress.Loopback, 6669);
            c.UpdateWaitTime = 0;

            Thread.Sleep(SleepTime);

            s.SendToAll(obj);

            T objec = c.WaitForPullObject<T>();

            Thread.Sleep(SleepTime);

            Assert.Equal(obj, objec);
            s.Close();
            c.Clear();
        }

        [Theory]
        [InlineData(55, 9.999e9)]
        void ServerRecievesObjectsFromClients<T1, T2>(T1 Test1, T2 Test2)
        {
            Server s = new Server(IPAddress.Loopback, 6669, 2);
            s.StartServer();

            Client c1 = new Client();
            c1.Connect(IPAddress.Loopback, 6669);

            Client c2 = new Client();
            c2.Connect(IPAddress.Loopback, 6669);

            c1.SendObject<T1>(Test1);
            c2.SendObject<T2>(Test2);

            T1 o1 = s.WaitForPullFromClient<T1>(0);
            T2 o2 = s.WaitForPullFromClient<T2>(1);

            Assert.True(Test1.Equals(o1) && Test2.Equals(o2));
        }

        [Fact]
        void ClientKeepsDataAfterDisconnect()
        {
            Server s = new Server(IPAddress.Loopback, 6669, 1);
            s.StartServer();

            Client c1 = new Client();
            c1.Connect(IPAddress.Loopback, 6669);

            Thread.Sleep(250);

            s.SendToAll("GIN");

            Thread.Sleep(SleepTime);

            c1.Disconnect();

            Assert.Equal("GIN", c1.PullObject<object>());
        }

        [Fact]
        void ServerHoldsToClientCap()
        {
            Server s = new Server(IPAddress.Loopback, 6669, 2);
            s.StartServer();

            Client c1 = new Client();
            c1.BeginConnect(IPAddress.Loopback, 6669);

            Client c2 = new Client();
            c2.BeginConnect(IPAddress.Loopback, 6669);

            Client c3 = new Client();
            c3.BeginConnect(IPAddress.Loopback, 6669);

            Thread.Sleep(SleepTime);

            Assert.Equal(2, s.ClientCount);
        }

        [Fact]
        void ServerRestartsOnDisconnect()
        {
            Server s = new Server(IPAddress.Loopback, 6669, 2);
            s.StartServer();
            s.RestartAutomatically = true;
            s.OnClientDisconnect += TestClass_OnDisconnect;
            s.UpdateWaitTime = 0;
            s.UpdateClientWaitTime = 0;

            Client c1 = new Client();
            c1.BeginConnect(IPAddress.Loopback, 6669);
            //c1.OnDisconnect += TestClass_OnDisconnect;
            c1.OnConnect += C1_OnConnect;
            c1.UpdateWaitTime = 0;

            Client c2 = new Client();
            c2.BeginConnect(IPAddress.Loopback, 6669);

            Client c3 = new Client();
            c3.BeginConnect(IPAddress.Loopback, 6669);

            Thread.Sleep(SleepTime);

            c1.Disconnect();
            Thread.Sleep(SleepTime);

            Assert.True(clientsDown == 1 && s.ClientCount == 2);
        }

        [Fact]
        void ServerDetectsForcibleDisconnection()
        {
            Client c = new Client();
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
    }
}
