using System;
using System.Collections.Generic;
using System.Linq;
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

        public bool Running = true;
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

                            try
                            {
                                OnConnect?.Invoke(connectionInfo);
                            }
                            catch
                            {

                            }
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
            {
                List<byte> bytes = new List<byte>(ObjectParser.ObjectToBytes(obj));
                // string s = ObjectParser.BytesToJson(bytes.ToArray());
                bytes.AddRange(ObjectParser.ObjectToBytes(";"));
                // s = ObjectParser.BytesToJson(bytes.ToArray());
                Connection.Send(bytes.ToArray());
            }
                //Connection.Send(ObjectParser.JsonToBytes(ObjectParser.ObjectToJson(obj) + ";"));
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
                if (ObjectParser.IsType<T>(ObjectQueue[i]))
                {
                    byte[] bytes = ObjectQueue[i];

                    ObjectQueue.RemoveAt(i);

                    return ObjectParser.BytesToObject<T>(bytes);
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
                    if (ObjectParser.IsType<T>(ObjectQueue[i]))
                    {
                        byte[] bytes = ObjectQueue[i];

                        ObjectQueue.RemoveAt(i);

                        return ObjectParser.BytesToObject<T>(bytes);
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
                if (ObjectParser.IsType<T>(ObjectQueue[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private void Recieve()
        {
            try
            {
                if (IsConnected && Connection.Available > 0)
                {
                    byte[] bytes = GetAllBytes();

                    if (bytes != null)
                    {
                        string json = ObjectParser.BytesToJson(bytes);

                        List<string> Sections = json.Split(new string[] { "\";\"" }, StringSplitOptions.None).Where(x => x != "" && x.Trim('"').Length > 0).ToList();

                        for (int i = 0; i < Sections.Count; i++)
                        {
                            string j = Sections[i];
                            byte[] b = ObjectParser.JsonToBytes(j);

                            if (ObjectParser.IsType<DisconnectionContext>(b))
                            {
                                DisconnectionContext ctx = ObjectParser.BytesToObject<DisconnectionContext>(b);
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
            }
            catch (SocketException)
            {
                Running = false;
                DisconnectedFrom(new DisconnectionContext() { type = DisconnectionContext.DisconnectionType.FORCIBLE });
            }
            catch (ObjectDisposedException) { }
        }

        private byte[] GetAllBytes()
        {
            List<byte> FullObject = new List<byte>();

            bool CompleteObject = false;

            while (!CompleteObject && Running)
            {
                while (Connection.Available == 0) ;
                byte[] bytes = new byte[Connection.Available];

                int RecievedBytes = Connection.Receive(bytes);
                string json = ObjectParser.BytesToJson(bytes);

                if (json.Trim('"') != "")
                {
                    if (RecievedBytes < 65536) CompleteObject = true;
                    FullObject.AddRange(bytes);
                }
                else if (FullObject.Count == 0)
                {
                    CompleteObject = true;
                }
            }

            return FullObject.ToArray();
        }

        private void ManagementLoop()
        {
            while (IsConnected)
            {
                Recieve();
                Thread.Sleep(UpdateWaitTime);
            }
        }

        private void DisconnectedFrom(DisconnectionContext ctx)
        {
            bool remove =
                ctx.type == DisconnectionContext.DisconnectionType.REMOVE ||
                (ctx.type == DisconnectionContext.DisconnectionType.FORCIBLE &&
                GlobalDefaults.ForcibleDisconnectMode == GlobalDefaults.ForcibleDisconnectBehavior.REMOVE);

            DisconnectionMode = ctx;

            try
            {
                OnDisconnect?.Invoke(DisconnectionMode, connectionInfo);
            }
            catch
            {

            }

            Connection?.Close();

            IsConnected = false;

            if (remove)
            {
                Running = false;
                ObjectQueue?.Clear();
            }
        }
    }
}
