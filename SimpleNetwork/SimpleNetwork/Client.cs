using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SimpleNetwork
{
    public class Client
    {
        private Thread BackgroundWorker;
        private Socket Connection;
        private List<object> ObjectQueue = new List<object>();
        private Dictionary<string, Type> NameTypeAssociations = new Dictionary<string, Type>();

        private RSAParameters RSAKey;
        byte[] Key;

        internal bool RecivedKey = false;

        public string[] Files
        {
            get 
            {
                string currentPath = Path.IsPathRooted(GlobalDefaults.FilePath) ?
                        GlobalDefaults.FilePath : Directory.GetCurrentDirectory() + GlobalDefaults.FilePath;
                return Directory.Exists(currentPath) ? Directory.GetFiles(currentPath) : null;
            }
        }

        public int UpdateWaitTime = 1000;
        public int QueuedObjectCount => ObjectQueue.Count;
        public ConnectionInfo connectionInfo { get; private set; }

        public bool Running = true;
        public bool IsConnected { get; private set; } = false;

        public DisconnectionContext DisconnectionMode { get; private set; }

        public delegate void Disconnected(DisconnectionContext ctx, ConnectionInfo inf);
        /// <summary>
        /// Called when the client is disconnected from
        /// </summary>
        public event Disconnected OnDisconnect;

        public delegate void RecievedFile(string path);
        public delegate void Connected(ConnectionInfo inf);
        internal delegate void RecievedFileServer(string path, ConnectionInfo info);
        /// <summary>
        /// Triggered when the BeginConnect finishes
        /// </summary>
        public event Connected OnConnect;
        public event RecievedFile OnFileRecieve;
        internal event RecievedFileServer OnServerRecieve;

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
                BackgroundWorker.IsBackground = true;
                BackgroundWorker.Start();
            }
            if (GlobalDefaults.UseEncryption)
            {
                RSAParameters PublicKey;
                CryptoServices.GenerateKeyPair(out PublicKey, out RSAKey);
                SendObject(PublicKey);
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

            BackgroundWorker.IsBackground = true;
            BackgroundWorker.Start();

            if (GlobalDefaults.UseEncryption)
                while (!RecivedKey) ;
        }

        /// <summary>
        /// Connects to server with specified address and port
        /// </summary>
        /// <param name="address">Ip address of the server. If invalid address is entered, an exception is thrown.</param>
        /// <param name="port">port number of the server</param>
        public void Connect(string address, int port)
        {
            Connect(IPAddress.Parse(address), port);
        }

        public async Task ConnectAsync(IPAddress address, int port)
        {
            await Task.Run(() => Connect(address, port)).ConfigureAwait(false);
        }

        public async Task ConnectAsync(string address, int port)
        {
            await ConnectAsync(IPAddress.Parse(address), port).ConfigureAwait(false);
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
                while (!IsConnected)
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

                ManagementLoop();
            });
            BackgroundWorker.IsBackground = true;
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
            BeginConnect(IPAddress.Parse(address), port);
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
            if (GlobalDefaults.UseEncryption && !RecivedKey && Key == null && !typeof(T).Equals(typeof(RSAParameters)))
                while (!RecivedKey) ;

            lock (LockObject)
                if (IsConnected)
                {
                    byte[] bytes = ObjectParser.ObjectToBytes(obj);
                    if (GlobalDefaults.UseEncryption)
                    {
                        if (!RecivedKey)
                        {
                            if (Key != null)
                                bytes = CryptoServices.EncryptRSA(bytes, RSAKey);
                        }
                        else
                        {
                            bytes = CryptoServices.EncryptAES(bytes, Key);
                        }
                    }
                    Connection.Send(ObjectContainer.Encapsulate(bytes, typeof(T).Name));
                }
        }

        public async Task SendObjectAsync<T>(T obj)
        {
            if (!RecivedKey && Key == null && !typeof(T).Equals(typeof(RSAParameters)))
                await Task.Run(() => 
                { 
                    while (!RecivedKey) ;
                });

            Task t = null;

            lock (LockObject)
                if (IsConnected)
                {
                    byte[] bytes = ObjectParser.ObjectToBytes(obj);
                    if (GlobalDefaults.UseEncryption)
                    {
                        if (!RecivedKey)
                        {
                            if (Key != null)
                                bytes = CryptoServices.EncryptRSA(bytes, RSAKey);
                        }
                        else
                        {
                            bytes = CryptoServices.EncryptAES(bytes, Key);
                        }
                    }
                    t = Task.Run(() =>
                    {
                        Connection.Send(ObjectContainer.Encapsulate(bytes, typeof(T).Name));
                    });
                }

            await t.ConfigureAwait(false);
        }

        public void SendFile(string path, string name = null)
        {
            name = name != null ? name + Path.GetExtension(path) : Path.GetFileName(path);

            int size = 2048;

            using (FileStream fs = File.OpenRead(path))
            {
                byte[] bytes = new byte[size];
                int bit;
                int i = 0;
                while ((bit = fs.ReadByte()) != -1)
                {
                    if (i == size)
                    {
                        i = 0;
                        SendFileSegment(bytes, name, fs.Length);
                        bytes = new byte[size];
                    }
                    bytes[i] = (byte)bit;
                    i++;
                }
                byte[] lastBytes = new byte[i];
                for (int j = 0; j < i; j++)
                {
                    lastBytes[j] = bytes[j];
                }
                SendFileSegment(bytes, name, fs.Length);
            }
        }

        public async Task SendFileAsync(string path, string name = null)
        {
            name = name != null ? name + Path.GetExtension(path) : Path.GetFileName(path);

            int size = 2048;

            using (FileStream fs = File.OpenRead(path))
            {
                byte[] bytes = new byte[size];
                int bit;
                int i = 0;
                while ((bit = fs.ReadByte()) != -1)
                {
                    if (i == size)
                    {
                        i = 0;
                        await Task.Run(() => SendFileSegment(bytes, name, fs.Length)).ConfigureAwait(false);
                        bytes = new byte[size];
                    }
                    bytes[i] = (byte)bit;
                    i++;
                }
                byte[] lastBytes = new byte[i];
                for (int j = 0; j < i; j++)
                {
                    lastBytes[j] = bytes[j];
                }
                await Task.Run(() => SendFileSegment(bytes, name, fs.Length)).ConfigureAwait(false);
            }
        }

        private void SendFileSegment(byte[] bytes, string name, long length)
        {
            lock (LockObject)
            {
                if (GlobalDefaults.UseEncryption)
                {
                    bytes = CryptoServices.EncryptAES(bytes, Key);
                }
                Connection.Send(ObjectContainer.Encapsulate(bytes, name, length));
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
            while (GlobalDefaults.UseEncryption && !RecivedKey) ;

            lock (LockObject)
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

        public async Task<T> PullObjectAsync<T>()
        {
            var t = Task.Run(() => WaitForPullObject<T>());
            return await t;
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
                while (GlobalDefaults.UseEncryption && !RecivedKey) ;
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

        public async Task<object[]> GetQueueObjectsTypelessAsync(bool clear = false)
        {
            return await Task.Run(() => GetQueueObjectsTypeless(clear)).ConfigureAwait(false);
        }

        public async Task<T[]> GetQueueObjectsTypedAsync<T>(bool clear = false)
        {
            return await Task.Run(() => GetQueueObjectsTyped<T>(clear)).ConfigureAwait(false);
        }

        #endregion
        #endregion

        private void Recieve()
        {
            IEnumerator<object> coroutine = RecieveCoroutine();
            while (Running)
            {
                RecieveCoroutine().MoveNext();
            }
            coroutine.Dispose();
        }

        private IEnumerator<object> RecieveCoroutine()
        {
            List<ObjectContainer> objects = new List<ObjectContainer>();

            byte[] Temp = null;

            while (Running)
            {
                while (Connection.Available == 0) yield return null;
                byte[] Buffer = new byte[Connection.Available];

                Connection.Receive(Buffer);

                if (Temp != null)
                {
                    List<byte> bytes = new List<byte>(Temp);
                    bytes.AddRange(Buffer);
                    Buffer = bytes.ToArray();

                }

                ObjectContainer[] obj = ObjectContainer.GetPackets(ref Buffer);
                Temp = Buffer;

                if (obj != null)
                    objects.AddRange(obj);

                if (Temp == null) break;
            }
            foreach (var item in objects)
            {
                Type type = item.Type != null ? GetTypeFromName(item.Type) : null;

                if (!RecivedKey && GlobalDefaults.UseEncryption)
                {
                    if (type.Equals(typeof(RSAParameters)))
                    {
                        RSAKey = (RSAParameters)ObjectParser.BytesToObject(item.content, type);
                        Key = CryptoServices.CreateHash(Guid.NewGuid().ToByteArray());

                        SendObject(Key);
                        RecivedKey = true;
                    }
                    else if (type.Equals(typeof(byte[])))
                    {
                        Key = CryptoServices.DecryptRSA(item.content, RSAKey);
                        Key = (byte[])ObjectParser.BytesToObject(Key, type);
                        RecivedKey = true;
                    }
                }
                else
                {
                    byte[] content = item.content;

                    if (GlobalDefaults.UseEncryption)
                    {
                        content = CryptoServices.DecryptAES(item.content, Key);
                    }

                    if (type == null)
                    {
                        long length = 0;
                        string dir = GlobalDefaults.FileDirectory;
                        FileStream fs;

                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        if (!File.Exists($@"{dir}\{item.fileInfo.Name}"))
                            fs = File.Create($@"{dir}\{item.fileInfo.Name}");
                        else
                            fs = new FileStream($@"{dir}\{item.fileInfo.Name}", FileMode.Append);

                        fs.Write(content, 0, content.Length);
                        fs.Flush();
                        length = fs.Length;
                        fs.Dispose();

                        if (length >= item.fileInfo.Length)
                        {
                            OnFileRecieve?.Invoke($@"{dir}\{item.fileInfo.Name}");
                            OnServerRecieve?.Invoke($@"{dir}\{item.fileInfo.Name}", connectionInfo);
                        }
                    }
                    else
                    {
                        if (type == typeof(DisconnectionContext))
                        {
                            DisconnectionContext ctx;
                            if (GlobalDefaults.UseEncryption && !RecivedKey)
                            {
                                ctx = new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE };
                            }
                            else
                            {
                                ctx = ObjectParser.BytesToObject<DisconnectionContext>(content);
                            }

                            DisconnectedFrom(ctx);
                        }
                        else
                        {
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
                                ObjectQueue.Add(ObjectParser.BytesToObject(content, type));
                            }
                        }
                    }
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
            IsConnected = false;
            try
            {
                OnDisconnect?.Invoke(DisconnectionMode, connectionInfo);
            }
            catch
            {

            }

            Running = false;
            IsConnected = false;
            Connection?.Close();

            if (remove)
            {
                ObjectQueue?.Clear();
            }
        }
    }
}