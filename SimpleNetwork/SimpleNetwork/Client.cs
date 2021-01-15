using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;

namespace SimpleNetwork
{
    public class Client
    {
        private Thread BackgroundWorker;
        private Socket Connection;
        private List<object> ObjectQueue = new List<object>();
        private Dictionary<string, Type> NameTypeAssociations = new Dictionary<string, Type>();

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

        #region User methods

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

        #region ServerUse

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

        /// <summary>
        /// Clears the queue. Use this if it is getting congested and the data is not important.
        /// </summary>
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
                    Connection.Send(ObjectHeader.AddHeadder(ObjectParser.ObjectToBytes(obj), typeof(T)));
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

        /// <summary>
        /// Returns all the objects in the queue
        /// </summary>
        /// <param name="clear">specifies whether or not ot clear the queue</param>
        /// <returns></returns>
        public object[] GetQueueObjectsTypeless(bool clear = false)
        {
            object[] arr;
            lock (LockObject)
            {
                arr = ObjectQueue.ToArray();
                if (clear) ClearQueue();
            }

            return arr;
        }

        /// <summary>
        /// Gets all objects in the queue of specified type
        /// </summary>
        /// <typeparam name="T">Type to return</typeparam>
        /// <param name="clear">Specifies whetehr or not to clear these items from the queue</param>
        /// <returns></returns>
        public T[] GetQueueObjectsTyped<T>(bool clear = false)
        {
            List<T> objects = new List<T>();

            lock (LockObject)
            {
                for (int i = 0; i < ObjectQueue.Count; i++)
                {
                    if (ObjectQueue[i].GetType() == typeof(T))
                    {
                        objects.Add((T)ObjectQueue[i]);
                    }
                    if (clear)
                    {
                        ObjectQueue.RemoveAt(i);
                        i--;
                    }
                }
            }
            return objects.ToArray();
        }

        #endregion
        #endregion

        private void Recieve()
        {
            bool CompleteObject = false;

            List<(ObjectHeader, List<byte>)> headerObjectPairs = new List<(ObjectHeader, List<byte>)>();

            while (!CompleteObject && Running)
            {
                while (Connection.Available == 0) ;
                byte[] Buffer = new byte[Connection.Available];

                int RecievedBytes = Connection.Receive(Buffer);

                List<ObjectHeader> headers;
                List<byte[]> objects = ObjectHeader.GetObjects(Buffer, out headers);

                int HeaderStart = headerObjectPairs.Count - 1;

                if (HeaderStart > -1 && headerObjectPairs[HeaderStart].Item2.Count != headerObjectPairs[HeaderStart].Item1.Length)
                {
                    headerObjectPairs[HeaderStart].Item2.AddRange(objects[objects.Count - 1]);
                    objects.RemoveAt(objects.Count - 1);
                }
                foreach (ObjectHeader header in headers)
                {
                    headerObjectPairs.Add((header, new List<byte>(objects[0])));
                    objects.RemoveAt(0);
                }
                foreach (var pair in headerObjectPairs)
                {
                    if (pair.Item2.Count == pair.Item1.Length)
                    {
                        CompleteObject = true;
                        break;
                    }
                }
            }
            int i = 0;
            foreach (var pair in headerObjectPairs)
            {
                if (pair.Item1.Type != typeof(DisconnectionContext).Name)
                {
                    Type type = GetTypeFromName(pair.Item1.Type);
                    lock (LockObject)
                    {
                        if (GlobalDefaults.OverwritePreviousOfTypeInQueue)
                        {
                            for (int j = 0; j < ObjectQueue.Count; j++)
                            {
                                if (ObjectQueue[j].GetType() == type)
                                {
                                    ObjectQueue.RemoveAt(j);
                                    j--;
                                }
                            }
                        }
                        ObjectQueue.Add(ObjectParser.BytesToObject(pair.Item2.ToArray(), type));
                    }
                    i++;
                }
                else
                {
                    DisconnectionContext ctx = ObjectParser.BytesToObject<DisconnectionContext>(pair.Item2.ToArray());
                    DisconnectedFrom(ctx);
                }
            }
        }

        private IEnumerator<object> RecieveCoroutine()
        {
            bool CompleteObject = false;

            List<(ObjectHeader, List<byte>)> headerObjectPairs = new List<(ObjectHeader, List<byte>)>();

            while (!CompleteObject && Running)
            {
                while (Connection.Available == 0) yield break;
                byte[] Buffer = new byte[Connection.Available];

                int RecievedBytes = Connection.Receive(Buffer);

                List<ObjectHeader> headers;
                List<byte[]> objects = ObjectHeader.GetObjects(Buffer, out headers);

                int HeaderStart = headerObjectPairs.Count - 1;

                if (HeaderStart > -1 && headerObjectPairs[HeaderStart].Item2.Count != headerObjectPairs[HeaderStart].Item1.Length)
                {
                    headerObjectPairs[HeaderStart].Item2.AddRange(objects[objects.Count - 1]);
                    objects.RemoveAt(objects.Count - 1);
                }
                foreach (ObjectHeader header in headers)
                {
                    headerObjectPairs.Add((header, new List<byte>(objects[0])));
                    objects.RemoveAt(0);
                }
                foreach (var pair in headerObjectPairs)
                {
                    if (pair.Item2.Count == pair.Item1.Length)
                    {
                        CompleteObject = true;
                        break;
                    }
                }
            }
            int i = 0;
            foreach (var pair in headerObjectPairs)
            {
                if (pair.Item1.Type != typeof(DisconnectionContext).Name)
                {
                    Type type = GetTypeFromName(pair.Item1.Type);
                    lock (LockObject)
                    {
                        if (GlobalDefaults.OverwritePreviousOfTypeInQueue)
                        {
                            for (int j = 0; j < ObjectQueue.Count; j++)
                            {
                                if (ObjectQueue[j].GetType() == type)
                                {
                                    ObjectQueue.RemoveAt(j);
                                    j--;
                                }
                            }
                        }
                        ObjectQueue.Add(ObjectParser.BytesToObject(pair.Item2.ToArray(), type));
                    }
                    i++;
                }
                else
                {
                    DisconnectionContext ctx = ObjectParser.BytesToObject<DisconnectionContext>(pair.Item2.ToArray());
                    DisconnectedFrom(ctx);
                }
            }
        }

        private Type GetTypeFromName(string name)
        {
            if (NameTypeAssociations.ContainsKey(name))
                return NameTypeAssociations[name];
            else
            {
                Type t = Utilities.ResolveTypeFromName(name);
                NameTypeAssociations.Add(name, t);
                return t;
            }
        }

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
                RecieveCoroutine().MoveNext();
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
