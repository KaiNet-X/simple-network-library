using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace SimpleNetwork
{
    public class Server
    {
        private readonly IPAddress Address;
        private readonly int Port;

        private Thread BackgroundWorker;
        private Socket ServerSocket;
        private List<Client> Clients = new List<Client>();
        //private List<bool> MarkedForRemove = new List<bool>();

        public int UpdateWaitTime = 10000;
        public int UpdateClientWaitTime = 1000;
        public readonly byte MaxClients;

        public byte ClientCount => (byte)Clients.Count;
        public bool Running { get; private set; } = false;
        public bool RestartAutomatically = false;
        private bool ModifyingClientList = false;

        public delegate void ClientDisconnected(DisconnectionContext ctx, ConnectionInfo inf);
        public event ClientDisconnected OnClientDisconnect;

        public delegate void ClientConnected(ConnectionInfo inf);
        public event ClientConnected OnClientConnect;

        public ClientAccessor ReadonlyClients;

        public Server(IPAddress iPAddress, int PortNum, byte MaxClients)
        {
            Address = iPAddress;
            Port = PortNum;
            this.MaxClients = MaxClients;
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            ReadonlyClients = new ClientAccessor(Clients);
        }

        /// <summary>
        /// Enables the server to connect clients
        /// </summary>
        public void StartServer()
        {
            if (ServerSocket == null)
            {
                ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
                        Socket s = ServerSocket.Accept();
                        Client c = new Client(s);
                        Clients.Add(c);
                        OnClientConnect?.Invoke(c.connectionInfo);
                        c.OnDisconnect += ClientDisconnect;
                        c.UpdateWaitTime = UpdateClientWaitTime;

                    }
                    catch (SocketException)
                    {
                        break;
                    }
                }
                Running = false;
                ServerSocket.Close();
                ServerSocket = null;
            });
            clientAcceptor.Start();

            BackgroundWorker = new Thread(() =>
            {
                Running = true;
                ManagementLoop();
            });
            BackgroundWorker.Start();
        }

        /// <summary>
        /// Stops connecting clients
        /// </summary>
        public void StopServer()
        {
            Running = false;
            ServerSocket.Close();
        }

        /// <summary>
        /// Sends Object t to all clients
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">Object to send</param>
        public void SendToAll<T>(T obj)
        {
            for (int i = 0; i < ClientCount; i++)
            {
                Clients[i].SendObject(obj);
            }
        }

        /// <summary>
        /// Closes the server, disconnects and clears all clients
        /// </summary>
        public void Close()
        {
            Running = false;
            ServerSocket?.Close();
            DisconnectAllClients(true);
        }

        /// <summary>
        /// Sends an object T to a specific client based on index
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="obj">Object to send</param>
        /// <param name="index">Index of the client</param>
        public void SendToOne<T>(T obj, byte index)
        {
            Clients[index].SendObject(obj);
        }

        /// <summary>
        /// Checks if a client's queue cotains object of type T
        /// </summary>
        /// <typeparam name="T">Type of object to look for</typeparam>
        /// <param name="index">Index of client to check</param>
        /// <returns></returns>
        public bool ClientHasObjectType<T>(byte index)
        {
            return Clients[index].HasObjectType<T>();
        }

        /// <summary>
        /// Pulls an object T from client. 
        /// If the client does not contain an object of this type, returns default
        /// </summary>
        /// <typeparam name="T">Pulls an object(T) from the client</typeparam>
        /// <param name="index">index of client to pull from</param>
        /// <returns></returns>
        public T PullFromClient<T>(byte index)
        {
            return Clients[index].PullObject<T>();
        }

        /// <summary>
        /// Pulls an object from a client. Waits until object of specified type is availible before returning.
        /// </summary>
        /// <typeparam name="T">Specified object type</typeparam>
        /// <param name="index">index of client to search for</param>
        /// <returns></returns>
        public T WaitForPullFromClient<T>(byte index)
        {
            return Clients[index].WaitForPullObject<T>();
        }

        /// <summary>
        /// clears all of the disconnected cients
        /// </summary>
        public void ClearDisconnectedClients()
        {
            WaitToModify();
            for (int i = 0; i < ClientCount; i++)
            {
                if (!Clients[i].IsConnected)
                {
                    Clients.RemoveAt(i);
                    i--;
                }
            }
            ModifyingClientList = false;
        }

        /// <summary>
        /// Disconnects client at index
        /// </summary>
        /// <param name="index">index of client</param>
        /// <param name="remove">specifies whether the client will be removed</param>
        public void DisconnectClient(byte index, bool remove = false)
        {
            Clients[index].Disconnect();
            if (remove)
            {
                WaitToModify();
                Clients.RemoveAt(index);
                ModifyingClientList = false;
            }
        }

        /// <summary>
        /// Disconnects a client while allowing you to specify disconnection context
        /// </summary>
        /// <param name="index">idex of client to disconnect</param>
        /// <param name="ctx">disconnection context to tell remote client how to handle disconnection</param>
        /// <param name="remove">specifies whether to remove the client</param>
        public void DisconnectClient(byte index, DisconnectionContext ctx, bool remove = false)
        {
            Clients[index].Disconnect(ctx);
            if (remove)
            {
                WaitToModify();
                Clients.RemoveAt(index);
                ModifyingClientList = false;
            }
        }

        /// <summary>
        /// disconnects all clients
        /// </summary>
        /// <param name="remove">specifies whether to remove the clients</param>
        public void DisconnectAllClients(bool remove = false)
        {
            WaitToModify();
            for (int i = 0; i < ClientCount; i++)
            {
                if (remove)
                {
                    try
                    {
                        Clients[i].Disconnect(new DisconnectionContext() { type = DisconnectionContext.DisconnectionType.REMOVE });
                    }
                    catch
                    {

                    }
                    Clients.RemoveAt(i);
                    i--;
                }
                else
                {
                    try
                    {
                        Clients[i].Disconnect();
                    }
                    catch
                    {

                    }
                }
                ModifyingClientList = false;
            }
        }

        private void ManagementLoop()
        {

        }

        private void WaitToModify()
        {
            while (ModifyingClientList)
            {

            }
            ModifyingClientList = true;
        }

        private void ClientDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            WaitToModify();
            for (int i = 0; i < ClientCount; i++)
            {
                if (Clients[i].IsConnected) continue;
                if (Clients[i].DisconnectionMode.type == DisconnectionContext.DisconnectionType.REMOVE)
                {
                    Clients.RemoveAt(i);
                    i--;
                }
                else if ((int)Clients[i].DisconnectionMode.type == 2 && (int)GlobalDefaults.ForcibleDisconnectMode == 0)
                {
                    Clients.RemoveAt(i);
                    i--;
                }
            }
            ModifyingClientList = false;

            if (RestartAutomatically && !Running && ClientCount < MaxClients) StartServer();
            OnClientDisconnect?.Invoke(ctx, inf);
        }

        public class ClientAccessor
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

                    return new ClientModel(inf, con);
                }
            }

            public class ClientModel
            {
                public readonly ConnectionInfo Info;
                public readonly bool IsConnected;

                internal ClientModel(ConnectionInfo info, bool connected)
                {
                    Info = info;
                    IsConnected = connected;
                }
            }
        }
    }
}
