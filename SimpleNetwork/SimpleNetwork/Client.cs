using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleNetwork
{
    public class Client
    {
        private Thread BackgroundWorker;
        private Socket Connection;
        private List<byte[]> ObjectQueue = new List<byte[]>();

        public int UpdateWaitTime = 1000;

        public ConnectionInfo connectionInfo { get; private set; }

        private bool isConnected = false;

        public bool Running 
        {
            get
            {
                if (isConnected)
                {
                    TestConnection();
                }
                return isConnected;
            }
            private set
            {
                isConnected = value;
            }
        }
        public bool IsConnected { get; private set; } = false;

        private bool TryingConnect = false;

        public DisconnectionContext DisconnectionMode { get; private set; }

        public delegate void Disconnected(DisconnectionContext ctx, ConnectionInfo inf);
        /// <summary>
        /// Called when the client is disconnected from
        /// </summary>
        public event Disconnected OnDisconnect;

        public delegate void Connected(ConnectionInfo inf);
        /// <summary>
        /// Triggered when the BeginConnect finishes
        /// </summary>
        public event Connected OnConnect;

        internal Client(Socket s)
        {
            Connection = s;
            IsConnected = true;

            IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
            IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

            connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);
            Running = true;

            BackgroundWorker = new Thread(() =>
            {
                ManagementLoop();
            });
            BackgroundWorker.Start();
        }

        public Client()
        {
            Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Connects to server with specified address and port
        /// </summary>
        /// <param name="address">Ip address of the server</param>
        /// <param name="port">port number of the server</param>

        public void Connect(IPAddress address, int port)
        {
            while (!IsConnected)
            {
                try
                {
                    if(!IsConnected)
                    {
                        Connection.Connect(new IPEndPoint(address, port));
                        IsConnected = true;
                        Running = true;

                        IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
                        IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

                        connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);
                    }
                }
                catch (SocketException) { }
            }

            BackgroundWorker = new Thread(() =>
            {
                ManagementLoop();
            });
            BackgroundWorker.Start();
        }

        /// <summary>
        /// Connects to server with specified address and port. 
        /// Allows code to continue when connecting.
        /// </summary>
        /// <param name="address">Ip address of the server</param>
        /// <param name="port">port number of the server</param>
        public void BeginConnect(IPAddress address, int port)
        {
            BackgroundWorker = new Thread(() =>
            {
                TryingConnect = true;
                while (!IsConnected && TryingConnect)
                {
                    try
                    {
                        if (!IsConnected)
                        {
                            Connection.Connect(new IPEndPoint(address, port));

                            IsConnected = true;
                            Running = true;

                            IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
                            IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

                            connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);

                            OnConnect?.Invoke(connectionInfo);
                        }
                    }
                    catch (SocketException)
                    {
                        //IsConnected = false;
                    }
                    Thread.Sleep(UpdateWaitTime);
                }
                if (!TryingConnect) return;
                TryingConnect = false;

                ManagementLoop();
            });
            BackgroundWorker.Start();
        }

        /// <summary>
        /// Cancels BeginConnect.
        /// </summary>
        public void CancelConnect()
        {
            TryingConnect = false;
        }

        /// <summary>
        /// Disconnects from server with default DisconnectionContext.
        /// </summary>
        public void Disconnect()
        {
            Disconnect(new DisconnectionContext());
        }

        /// <summary>
        /// Disconnects from server while allowing you to specify the DisconnectionContext.
        /// </summary>
        /// <param name="ctx"></param>
        public void Disconnect(DisconnectionContext ctx)
        {
            try
            {
                SendObject(ctx);
            }
            catch
            {

            }

            DisconnectionMode = ctx;
            IsConnected = false;

            if (ctx.type == DisconnectionContext.DisconnectionType.REMOVE)
            {
                Running = false;
                ObjectQueue.Clear();
            }
        }

        /// <summary>
        /// Closes the connection and clears the queue. 
        /// Use this when disconnected by server to clear the resources.
        /// </summary>
        public void Clear()
        {
            try
            {
                Connection?.Close();
            }
            catch
            {

            }

            IsConnected = false;
            Running = false;

            ObjectQueue.Clear();
            ObjectQueue = null;
        }

        /// <summary>
        /// Sends object of type T to the server.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="obj">Object of any type</param>
        public void SendObject<T>(T obj)
        {
            if (IsConnected)
                Connection.Send(JsonObjectParser.JsonToBytes(JsonObjectParser.ObjectToJson(obj) + ";"));
        }

        /// <summary>
        /// Pulls the first object(T) in FIFO order.
        /// If there is no object(T) in the queue.
        /// </summary>
        /// <typeparam name="T">Type of object to pull.</typeparam>
        /// <returns>First occurrence of T in queue. If it does not exist, return default.</returns>
        public T PullObject<T>()
        {
            for (int i = 0; i < ObjectQueue.Count; i++)
            {
                if (JsonObjectParser.IsType<T>(ObjectQueue[i]))
                {
                    byte[] bytes = ObjectQueue[i];

                    ObjectQueue.RemoveAt(i);

                    return JsonObjectParser.BytesToObject<T>(bytes);
                }
            }
            return default;
        }

        /// <summary>
        /// Waits until there is an object(T), and then pulls it.
        /// </summary>
        /// <typeparam name="T">Type of object to pull</typeparam>
        /// <returns>object(T)</returns>
        public T WaitForPullObject<T>()
        {           
            while (Running)
            {
                for (int i = 0; i < ObjectQueue.Count; i++)
                {
                    if (JsonObjectParser.IsType<T>(ObjectQueue[i]))
                    {
                        byte[] bytes = ObjectQueue[i];

                        ObjectQueue.RemoveAt(i);

                        return JsonObjectParser.BytesToObject<T>(bytes);
                    }
                }
                Thread.Sleep(UpdateWaitTime);
            }

            return PullObject<T>();
        }

        /// <summary>
        /// Checks if an object(T) is in 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasObjectType<T>()
        {
            for (int i = 0; i < ObjectQueue.Count; i++)
            {
                if (JsonObjectParser.IsType<T>(ObjectQueue[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void RecieveDeprecated()
        {
            try
            {
                if (IsConnected && Connection.Available > 0)
                {
                    byte[] get = new byte[Connection.Available];
                    int ConnectCheck = Connection.Receive(get);

                    string json = JsonObjectParser.BytesToJson(get);
                    List<string> Sections = new List<string>(json.Split(';'));

                    for (int i = 0; i < Sections.Count; i++)
                    {
                        if (Sections[i] == "" || Sections[i] == "\"\"")
                        {
                            Sections.RemoveAt(i);
                            i--;
                        }
                    }

                    for (int i = 0; i < Sections.Count; i++)
                    {
                        string j = Sections[i];
                        byte[] b = JsonObjectParser.JsonToBytes(j);

                        if (JsonObjectParser.IsType<DisconnectionContext>(b))
                        {
                            DisconnectionContext ctx = JsonObjectParser.JsonToObject<DisconnectionContext>(j);
                            DisconnectionMode = ctx;

                            Running = false;
                            IsConnected = false;

                            OnDisconnect?.Invoke(ctx, connectionInfo);

                            if (ctx.type == DisconnectionContext.DisconnectionType.REMOVE)
                            {
                                Clear();
                            }
                            else
                            {
                                Connection.Close();
                            }
                            return;
                        }
                        else
                        {
                            ObjectQueue.Add(b);
                        }
                    }
                }
            }
            catch (SocketException)
            {
                DisconnectedFrom(new DisconnectionContext() { type = DisconnectionContext.DisconnectionType.FORCIBLE });
            }
        }

        private void Recieve()
        {
            try
            {
                if (IsConnected && Connection.Available > 0)
                {
                    byte[] bytes = GetAllBytes();

                    string json = JsonObjectParser.BytesToJson(bytes);
                    List<string> Sections = new List<string>(json.Split(';'));

                    for (int i = 0; i < Sections.Count; i++)
                    {
                        if (Sections[i] == "" || Sections[i] == "\"\"")
                        {
                            Sections.RemoveAt(i);
                            i--;
                        }
                    }

                    for (int i = 0; i < Sections.Count; i++)
                    {
                        string j = Sections[i];
                        byte[] b = JsonObjectParser.JsonToBytes(j);

                        if (JsonObjectParser.IsType<DisconnectionContext>(b))
                        {
                            DisconnectionContext ctx = JsonObjectParser.JsonToObject<DisconnectionContext>(j);
                            DisconnectedFrom(ctx);

                            return;
                        }
                        else
                        {
                            ObjectQueue.Add(b);
                        }
                    }
                }
            }
            catch (SocketException)
            {
                DisconnectedFrom(new DisconnectionContext() { type = DisconnectionContext.DisconnectionType.FORCIBLE });
            }
        }

        private byte[] GetAllBytes()
        {
            List<byte> FullObject = new List<byte>();

            bool CompleteObject = false;

            while (!CompleteObject && Running)
            {
                while (Connection.Available == 0) ;
                byte[] bytes = new byte[Connection.Available];

                Connection.Receive(bytes);
                string json = JsonObjectParser.BytesToJson(bytes);

                if (json[json.Length - 1] == '}' || json[json.Length - 1] == ';')
                {
                    CompleteObject = true;
                }
                FullObject.AddRange(bytes);
            }

            return FullObject.ToArray();
        }

        private void TestConnection()
        {
            if (IsConnected)
            {
                try
                {
                    SendObject("");
                }
                catch
                {
                    DisconnectedFrom(new DisconnectionContext() { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                    return;
                }
            }
        }

        private void DisconnectedFrom(DisconnectionContext ctx)
        {
            bool remove =
                ctx.type == DisconnectionContext.DisconnectionType.REMOVE ||
                (ctx.type == DisconnectionContext.DisconnectionType.FORCIBLE &&
                GlobalDefaults.ForcibleDisconnectMode == GlobalDefaults.ForcibleDisconnectBehavior.REMOVE);

            DisconnectionMode = ctx;

            OnDisconnect?.Invoke(DisconnectionMode, connectionInfo);

            Connection?.Close();

            IsConnected = false;

            if (remove)
            {
                Running = false;
                ObjectQueue.Clear();
            }
        }

        private void ManagementLoop()
        {
            while (IsConnected)
            {
                TestConnection();
                Recieve();
                Thread.Sleep(UpdateWaitTime);
            }
        }
    }
}
