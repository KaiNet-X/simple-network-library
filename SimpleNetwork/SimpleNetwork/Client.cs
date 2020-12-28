using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleNetwork
{
    public class Client
    {
        private Thread BackgroundWorker;
        private Socket Connection;
        private List<object> ObjectQueue = new List<object>();

        public int UpdateWaitTime = 1000;
        public int QueuedObjectCount => ObjectQueue.Count;
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

        private readonly object LockObject = new object();

        internal Client(Socket s)
        {
            Connection = s;
            IsConnected = true;

            IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
            IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

            connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);
            Running = true;
            if (!GlobalDefaults.RunServerClientsOnOneThread)
            {
                BackgroundWorker = new Thread(() =>
                {
                    ManagementLoop();
                });
                BackgroundWorker.Start();
            }
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
        /// Connects to server with specified address and port
        /// </summary>
        /// <param name="address">Ip address of the server. If invalid address is entered, an exception is thrown.</param>
        /// <param name="port">port number of the server</param>
        public void Connect(string address, int port)
        {
            while (!Connection.Connected)
            {
                try
                {
                    if (!IsConnected)
                    {
                        Connection.Connect(new IPEndPoint(IPAddress.Parse(address), port));
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
        /// Connects to server with specified address and port. 
        /// Allows code to continue when connecting.
        /// </summary>
        /// <param name="address">Ip address of the server. If invalid address is entered, an exception is thrown.</param>
        /// <param name="port">port number of the server</param>
        public void BeginConnect(string address, int port)
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
                            Connection.Connect(new IPEndPoint(IPAddress.Parse(address), port));

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

            Running = false;
            if (ctx.type == DisconnectionContext.DisconnectionType.REMOVE)
            {
                lock (LockObject)
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

            lock (LockObject)
            {
                ObjectQueue.Clear();
                ObjectQueue = null;
            }
        }

        public void ClearQueue()
        {
            lock (LockObject)
                ObjectQueue.Clear();
        }
        /// <summary>
        /// Sends object of type T to the server.
        /// </summary>
        /// <typeparam name="T">Any type</typeparam>
        /// <param name="obj">Object of any type</param>
        public void SendObject<T>(T obj)
        {
            lock(LockObject)
                if (IsConnected)
                {
                    Connection.Send(PacketHeader.AddHeadders(ObjectParser.ObjectToBytes(obj), typeof(T)));
                }
        }

        /// <summary>
        /// Pulls the first object(T) in FIFO order.
        /// If there is no object(T) in the queue.
        /// </summary>
        /// <typeparam name="T">Type of object to pull.</typeparam>
        /// <returns>First occurrence of T in queue. If it does not exist, return default.</returns>
        public T PullObject<T>()
        {
            lock(LockObject)
            {
                for (int i = 0; i < ObjectQueue.Count; i++)
                {
                    if (ObjectQueue[i].GetType().Equals(typeof(T)))
                    {
                        object o = ObjectQueue[i];

                        ObjectQueue.RemoveAt(i);

                        return (T)o;
                    }
                }
                return default;
            }
        }

        /// <summary>
        /// Waits until there is an object(T), and then pulls it.
        /// </summary>
        /// <typeparam name="T">Type of object to pull</typeparam>
        /// <returns>object(T)</returns>
        public T WaitForPullObject<T>()
        {
            do
            {
                lock (LockObject)
                    for (int i = 0; i < ObjectQueue.Count; i++)
                    {
                        if (ObjectQueue[i].GetType().Equals(typeof(T)))
                        {
                            object o = ObjectQueue[i];

                            ObjectQueue.RemoveAt(i);

                            return (T)o;
                        }
                    }
                Thread.Sleep(UpdateWaitTime);
            }
            while (Running);

            return PullObject<T>();
        }

        /// <summary>
        /// Checks if an object(T) is in 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasObjectType<T>()
        {
            lock(LockObject)
            {
                for (int i = 0; i < ObjectQueue.Count; i++)
                {
                    if (ObjectQueue[i].GetType().Equals(typeof(T)))
                    {
                        return true;
                    }
                }
                return false;
            }    
        }

        //private void Recieve2()
        //{
        //    try
        //    {
        //        if (IsConnected && Connection.Available > 0)
        //        {
        //            List<byte[]> objects = GetAllBytes();

        //            if (objects.Count > 0)
        //            {

        //                for (int i = 0; i < objects.Count; i++)
        //                {
        //                    string j = ObjectParser.BytesToJson(objects[i]);
        //                    byte[] b = objects[i];

        //                    if (ObjectParser.IsType<DisconnectionContext>(b))
        //                    {
        //                        DisconnectionContext ctx = ObjectParser.BytesToObject<DisconnectionContext>(b);
        //                        DisconnectedFrom(ctx);

        //                        return;
        //                    }
        //                    else
        //                    {
        //                        lock (LockObject)
        //                            ObjectQueue.Add(b);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    catch (SocketException)
        //    {
        //        Running = false;
        //        DisconnectedFrom(new DisconnectionContext() { type = DisconnectionContext.DisconnectionType.FORCIBLE });
        //    }
        //    catch (ObjectDisposedException) { }
        //}

        private void Recieve()
        {
            List<PacketHeader> Headers = new List<PacketHeader>();
            List<byte[]> Objects = new List<byte[]>();
            List<byte> FullObject = new List<byte>();

            bool CompleteObject = false;

            while (!CompleteObject && Running)
            {
                while (Connection.Available == 0) ;
                byte[] Buffer = new byte[Connection.Available];

                int RecievedBytes = Connection.Receive(Buffer);

                if (RecievedBytes < 65536)
                {
                    List<PacketHeader> headers;
                    List<byte[]> objects = PacketHeader.GetObjects(Buffer, out headers);
                    FullObject.AddRange(objects[0]);
                    objects.RemoveAt(0);
                    Objects.Add(FullObject.ToArray());
                    foreach (byte[] ob in objects)
                    {
                        Objects.Add(ob);
                    }
                    Headers.AddRange(headers);
                    CompleteObject = true;
                }
                else
                {
                    PacketHeader h = PacketHeader.GetHeader(Buffer);
                    Headers.Add(h);
                    FullObject.AddRange(PacketHeader.RemoveHeader(Buffer));
                    if (h.FinalPacket)
                        CompleteObject = true;
                }
            }
            int i = 0;
            foreach (PacketHeader h in Headers)
            {
                if (h.FinalPacket)
                {
                    if (h.Type != typeof(DisconnectionContext).FullName)
                    {
                        lock (LockObject)
                            ObjectQueue.Add(ObjectParser.BytesToObject(Objects[i], Type.GetType(h.Type)));
                        i++;
                    }
                    else
                    {
                        DisconnectionContext ctx = ObjectParser.BytesToObject<DisconnectionContext>(Objects[i]);
                        DisconnectedFrom(ctx);
                    }
                }
            }
        }

        //private List<byte[]> GetAllBytes()
        //{
        //    List<byte[]> Objects = new List<byte[]>();
        //    List<byte> FullObject = new List<byte>();

        //    bool CompleteObject = false;

        //    while (!CompleteObject && Running)
        //    {
        //        while (Connection.Available == 0) ;
        //        byte[] Buffer = new byte[Connection.Available];

        //        int RecievedBytes = Connection.Receive(Buffer);

        //        if (RecievedBytes < 65536)
        //        {
        //            List<PacketHeader> headers;
        //            List<byte[]> objects = PacketHeader.GetObjects(Buffer, out headers);
        //            FullObject.AddRange(objects[0]);
        //            objects.RemoveAt(0);
        //            Objects.Add(FullObject.ToArray());
        //            foreach (byte[] ob in objects)
        //            {
        //                Objects.Add(ob);
        //            }
        //            CompleteObject = true;
        //        }
        //        else
        //        {
        //            PacketHeader h = PacketHeader.GetHeader(Buffer);
        //            FullObject.AddRange(PacketHeader.RemoveHeader(Buffer));
        //            if (h.FinalPacket)
        //                CompleteObject = true;
        //        }
        //    }
        //    return Objects;
        //}

        private void ManagementLoop()
        {
            while (IsConnected)
            {
                Recieve();
                Thread.Sleep(UpdateWaitTime);
            }
        }

        internal void Manage()
        {
            if (IsConnected)
                Recieve();
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
