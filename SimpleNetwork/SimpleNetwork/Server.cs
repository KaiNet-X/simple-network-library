using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleNetwork
{
    public class Server
    {
        #region properties
        private readonly IPAddress Address;
        private readonly int Port;

        private Thread BackgroundWorker;
        private Socket ServerSocket;
        private List<Client> Clients = new List<Client>();

        public int ClientUpdateWaitTime = 150;
        public readonly ushort MaxClients;

        public ushort ClientCount => (ushort)Clients.Count;
        public bool Running { get; private set; } = false;
        public bool RestartAutomatically = false;

        public delegate void ClientDisconnected(DisconnectionContext ctx, ConnectionInfo inf);
        public delegate void ClientConnected(ConnectionInfo inf, ushort index);
        public delegate void RecievedFile(FileStream file, ConnectionInfo info);
        public delegate void RecievedObject(object obj, ConnectionInfo info);

        public event ClientDisconnected OnClientDisconnect;
        public event ClientConnected OnClientConnect;
        public event RecievedFile OnClientRecieveFile;
        public event RecievedObject OnClientRecieveObject;

        public ClientAccessor ReadonlyClients;

        private object LockObject = new object();

        private bool ConnectingClient = false;
        #endregion

        #region serverCode
        public Server(IPAddress iPAddress, int PortNum, ushort MaxClients)
        {
            Address = iPAddress;
            Port = PortNum;
            this.MaxClients = MaxClients;
            ServerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            ServerSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);

            ReadonlyClients = new ClientAccessor(Clients);
        }

        public Server(string iPAddress, int PortNum, ushort MaxClients) : this(IPAddress.Parse(iPAddress), PortNum, MaxClients)
        {
            
        }

        public void StartServer()
        {
            if (ServerSocket == null)
            {
                ServerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            }
            Thread clientAcceptor = new Thread(() =>
            {
                Running = true;
                ServerSocket.Bind(new IPEndPoint(Address, Port));
                ServerSocket.Listen(MaxClients);

                while (Running && Clients.Count < MaxClients)
                {
                    try
                    {
                        Client c = new Client(ServerSocket.Accept());

                        ConnectingClient = true;

                        if (Running)
                        {
                            c.OnDisconnect += ClientDisconnect;

                            if (OnClientRecieveFile != null)
                                c.OnFileRecieve += (FileStream file) => OnClientRecieveFile.Invoke(file, c.connectionInfo);
                            if (OnClientRecieveObject != null)
                                c.OnRecieveObject += (object obj) => OnClientRecieveObject?.Invoke(obj, c.connectionInfo);

                            c.UpdateWaitTime = ClientUpdateWaitTime;
                        }                                
                        else
                            c.Disconnect(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.REMOVE });
                       
                        lock(LockObject)
                            Clients.Add(c);

                        ConnectingClient = false;
                        if (c.IsConnected)
                            OnClientConnect?.Invoke(c.connectionInfo, (ushort)(ClientCount - 1));
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                    catch { }
                }
                Running = false;
                ServerSocket.Close();
                ServerSocket = null;
            });
            clientAcceptor.IsBackground = true;
            clientAcceptor.Start();

            Running = true;

            if (GlobalDefaults.RunServerClientsOnOneThread)
            {
                BackgroundWorker = new Thread(() =>
                {
                    ManagementLoop();
                });

                BackgroundWorker.IsBackground = true;
                BackgroundWorker.Start();
            }
        }

        public void StopServer()
        {
            Running = false;
            RestartAutomatically = false;
            ServerSocket?.Close();
        }

        public void Close()
        {
            Running = false;
            RestartAutomatically = false;
            ServerSocket?.Close();
            DisconnectAllClients(true);
        }

        #region clientAccess

        public void SendToAll<T>(T obj)
        {
            WaitForPendingConnections();
            for (int i = 0; i < ClientCount; i++)
            {
                while (GlobalDefaults.UseEncryption && !Clients[i].RecivedKey) ;
                lock (LockObject)
                    Clients[i].SendObject(obj);
            }
        }

        public async Task SendToAllAsync<T>(T obj)
        {
            List<Task> tasks = new List<Task>();

            WaitForPendingConnections();
            for (int i = 0; i < ClientCount; i++)
            {
                if (GlobalDefaults.UseEncryption)
                    await Task.Run(() =>
                    {
                        while (GlobalDefaults.UseEncryption && !Clients[i].RecivedKey) ;
                    });
                lock (LockObject)
                    tasks.Add(Clients[i].SendObjectAsync(obj));
            }

            foreach (Task t in tasks)
            {
                await t;
            }
        }

        public void SendToOne<T>(T obj, ushort index)
        {
            while (GlobalDefaults.UseEncryption && !Clients[index].RecivedKey) ;
            Clients[index].SendObject(obj);
        }

        public async Task SendToOneAsync<T>(T obj, ushort index)
        {
            await Task.Run(() =>
            {
                while (GlobalDefaults.UseEncryption && !Clients[index].RecivedKey) ;
            });
            await Clients[index].SendObjectAsync(obj);
        }

        public void SendFileToAll(string path, string name = null)
        {
            WaitForPendingConnections();
            for (int i = 0; i < ClientCount; i++)
            {
                while (GlobalDefaults.UseEncryption && !Clients[i].RecivedKey) ;
                lock (LockObject)
                    Clients[i].SendFile(path, name);
            }
        }

        public async Task SendFileToAllAsync(string path, string name = null)
        {
            List<Task> tasks = new List<Task>();

            WaitForPendingConnections();
            for (int i = 0; i < ClientCount; i++)
            {
                await Task.Run(() =>
                {
                    while (GlobalDefaults.UseEncryption && !Clients[i].RecivedKey) ;
                });
                lock (LockObject)
                tasks.Add(Clients[i].SendFileAsync(path, name));
            }

            foreach (Task t in tasks)
            {
                await t;
            }
        }

        public void SendFileToOne(string path, ushort index, string name = null)
        {
            while (GlobalDefaults.UseEncryption && !Clients[index].RecivedKey) ;
            lock (LockObject)
                Clients[index].SendFile(path, name);
        }

        public async Task SendFileToOneAsync(string path, ushort index, string name = null)
        {
            await Task.Run(() =>
            {
                while (GlobalDefaults.UseEncryption && !Clients[index].RecivedKey) ;
            });
            Task tsk = null;
            lock (LockObject)
                tsk =  Clients[index].SendFileAsync(path, name);
            await tsk;
        }

        public bool ClientHasObjectType<T>(ushort index)
        {
            try
            {
                return Clients[index].HasObjectType<T>();
            }
            catch (AggregateException ex)
            {
                if (index < ClientCount)
                    return false;
                else throw ex;
            }
        }

        public T PullFromClient<T>(ushort index)
        {
            return Clients[index].PullObject<T>();
        }

        public T WaitForPullFromClient<T>(ushort index)
        {
            return Clients[index].WaitForPullObject<T>();
        }

        public async Task<T> PullFromClientAsync<T>(ushort index)
        {
            return await Clients[index].PullObjectAsync<T>();
        }

        public object[] GetClientQueueObjectsTypeless(ushort index, bool clear = false)
        {
            object[] obs;
            lock(LockObject)
            {
                obs = Clients[index].GetQueueObjectsTypeless(clear);
            }
            return obs;
        }

        public T[] GetClientQueueTyped<T>(ushort index, bool clear = false)
        {
            T[] obs;
            lock (LockObject)
            {
                obs = Clients[index].GetQueueObjectsTyped<T>(clear);
            }
            return obs;
        }

        public async Task<object[]> GetClientQueueObjectsTypelessAsync(ushort index, bool clear = false)
        {
            object[] obs;
            Task<object[]> tsk = null;
            lock (LockObject)
            {
                tsk = Clients[index].GetQueueObjectsTypelessAsync(clear);
            }
            return await tsk;
        }

        public async Task<T[]> GetClientQueueTypedAsync<T>(ushort index, bool clear = false)
        {
            Task<T[]> tsk = null;
            lock (LockObject)
            {
                tsk = Clients[index].GetQueueObjectsTypedAsync<T>(clear);
            }
            return await tsk;
        }

        public void ClearDisconnectedClients()
        {
            lock (LockObject)
            {
                for (int i = 0; i < ClientCount; i++)
                {
                    if (!Clients[i].IsConnected)
                    {
                        Clients.RemoveAt(i);
                        i--;
                    }
                }
                if (RestartAutomatically && !Running && ClientCount < MaxClients) StartServer();
            }
        }

        public void ClearClientQueue(ushort index) 
        {
            Clients[index].ClearQueue();
        }

        public void ClearAllQueue()
        {
            lock(LockObject)
            {
                foreach (Client c in Clients)
                    c.ClearQueue();
            }
        }

        public void DisconnectClient(ushort index, bool remove = false)
        {
            Clients[index].Disconnect();
            if (remove)
            {
                while (GlobalDefaults.UseEncryption && !Clients[index].RecivedKey) ;
                lock (LockObject)
                {
                    Clients.RemoveAt(index);
                    if (RestartAutomatically && !Running && ClientCount < MaxClients) StartServer();
                }
            }
        }

        public void DisconnectClient(ushort index, DisconnectionContext ctx, bool remove = false)
        {
            Clients[index].Disconnect(ctx);
            if (remove)
            {
                while (GlobalDefaults.UseEncryption && !Clients[index].RecivedKey) ;
                lock (LockObject)
                {
                    Clients.RemoveAt(index);
                    if (RestartAutomatically && !Running && ClientCount < MaxClients) StartServer();
                }
            }
        }

        public void DisconnectAllClients(bool remove = false)
        {
            DisconnectAllClients(new DisconnectionContext (), remove);
        }

        public void DisconnectAllClients(DisconnectionContext ctx, bool remove = false)
        {
            for (int i = 0; i < ClientCount; i++)
            {
                while (GlobalDefaults.UseEncryption && !Clients[i].RecivedKey) ;
                lock (LockObject)
                {
                    Clients[i].Disconnect(ctx);
                    if (remove)
                    {
                        Clients.RemoveAt(i);
                        i--;
                    }
                }
            }
            if (remove && RestartAutomatically && !Running && ClientCount < MaxClients) StartServer();
        }

        #endregion
        #endregion

        private void WaitForPendingConnections()
        {
            while (ConnectingClient) ;
        }

        private void ManagementLoop()
        {
            while (true)
            {
                lock (LockObject)
                    for (int i = 0; i < ClientCount; i++)
                    {
                        Clients[i].Manage();
                    }
                Thread.Sleep(ClientUpdateWaitTime);
            }
        }

        private void ClientDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            lock(LockObject)
            {
                for (int i = 0; i < ClientCount; i++)
                {
                    if (!Clients[i].connectionInfo.Equals(inf)) continue;
                    if (Clients[i].DisconnectionMode.type == DisconnectionContext.DisconnectionType.REMOVE)
                    {
                        Clients.RemoveAt(i);
                        i--;
                    }
                    else if ((int)Clients[i].DisconnectionMode.type == 2 && GlobalDefaults.ForcibleDisconnectMode == 0)
                    {
                        Clients.RemoveAt(i);
                        i--;
                    }
                }

                OnClientDisconnect?.Invoke(ctx, inf);
            }
            if (RestartAutomatically && !Running && ClientCount < MaxClients) StartServer();
        }

        public class ClientAccessor : IEnumerable<ClientAccessor.ClientModel>
        {
            private readonly List<Client> Clients;

            internal ClientAccessor(List<Client> clients)
            {
                Clients = clients;
            }

            public ClientModel this[int index]
            {
                get
                {
                    ConnectionInfo inf = Clients[index].connectionInfo;
                    bool con = Clients[index].IsConnected;
                    int count = Clients[index].QueuedObjectCount;
                    return new ClientModel(inf, con, count);
                }
            }

            public IEnumerator<ClientModel> GetEnumerator()
            {
                List<ClientModel> ac = new List<ClientModel>();
                for (int i = 0; i < Clients.Count; i++)
                {
                    ac.Add(this[i]);
                }
                return ac.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public class ClientModel
            {
                public readonly ConnectionInfo Info;
                public readonly bool IsConnected;
                public readonly int QueuedObjectCount;

                internal ClientModel(ConnectionInfo info, bool connected, int queuedObjectCount)
                {
                    Info = info;
                    IsConnected = connected;
                    QueuedObjectCount = queuedObjectCount;
                }
            }
        }
    }
}