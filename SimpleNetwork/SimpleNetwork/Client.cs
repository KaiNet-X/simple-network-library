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
        private volatile Socket Connection;
        private volatile List<object> ObjectQueue = new List<object>();

        private RSAParameters RSAKey;
        private byte[] Key;

        internal bool RecivedKey = false;

        public int UpdateWaitTime = 150;
        public int QueuedObjectCount => ObjectQueue.Count;
        public ConnectionInfo connectionInfo { get; private set; }

        public bool IsConnected { get; private set; } = false;

        public DisconnectionContext DisconnectionMode { get; private set; }

        public delegate void Disconnected(DisconnectionContext ctx, ConnectionInfo inf);
        public delegate void RecievedFile(SimpleFile file);
        public delegate void Connected(ConnectionInfo inf);
        public delegate bool ConnectException(SocketException ex, uint attempts);
        public delegate void RecievedObject(object obj);

        public event Disconnected OnDisconnect;
        public event Connected OnConnect;
        public RecievedFile OnFileRecieve;
        public event RecievedObject OnRecieveObject;
        public ConnectException OnConnectError;
        //internal event RecievedFileServer OnServerRecieve;

        private readonly object LockObject = new object();
        private IEnumerator<object> Reciever;
        internal Client(Socket s)
        {
            Connection = s;
            IsConnected = true;

            IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
            IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

            connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);

            Reciever = RecieveCoroutine();

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
            Connection = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
            Connection.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
            Reciever = RecieveCoroutine();
        }

        #region User methods

        public void Connect(IPAddress address, int port)
        {
            uint attempts = 1;
            while (!IsConnected)
            {
                try
                {
                    Connection.Connect(new IPEndPoint(address, port));
                    IsConnected = true;

                    IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
                    IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

                    connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);
                }
                catch (SocketException ex) 
                {
                    if (OnConnectError != null)
                    {
                        try
                        {
                            if (!OnConnectError.Invoke(ex, attempts))
                                return;
                        }
                        catch
                        {

                        }
                        attempts++;
                    }
                }
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

        public void BeginConnect(IPAddress address, int port)
        {
            BackgroundWorker = new Thread(() =>
            {
                uint attempts = 1;
                while (!IsConnected)
                {
                    try
                    {
                        Connection.Connect(new IPEndPoint(address, port));

                        IsConnected = true;

                        IPEndPoint loc = Connection.LocalEndPoint as IPEndPoint;
                        IPEndPoint rem = Connection.RemoteEndPoint as IPEndPoint;

                        connectionInfo = new ConnectionInfo(loc.Address, Dns.GetHostName(), rem.Address, Dns.GetHostEntry(rem.Address).HostName);

                        try
                        {
                            Task.Run(()=>OnConnect?.Invoke(connectionInfo));
                        }
                        catch
                        {

                        }
                    }
                    catch (SocketException ex)
                    {
                        if (OnConnectError != null)
                        {
                            try
                            {
                                if (!OnConnectError.Invoke(ex, attempts))
                                    return;
                            }
                            catch
                            {

                            }
                            attempts++;
                        }
                    }
                    Thread.Sleep(UpdateWaitTime);
                }

                ManagementLoop();
            });
            BackgroundWorker.IsBackground = true;
            BackgroundWorker.Start();
        }

        public void BeginConnect(string address, int port)
        {
            BeginConnect(IPAddress.Parse(address), port);
        }

        #region ServerUse

        public void Disconnect()
        {
            Disconnect(new DisconnectionContext(GlobalDefaults.DefaultContext));
        }

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
                lock (LockObject)
                    ObjectQueue.Clear();
            }
        }

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
                    try
                    {
                        Connection.Send(ObjectContainer.Encapsulate(bytes, typeof(T).Name));
                    }
                    catch (SocketException ex)
                    {
                        DisconnectedFrom(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                    }
                }
        }

        public async Task SendObjectAsync<T>(T obj)
        {
            if (GlobalDefaults.UseEncryption && !RecivedKey && Key == null && !typeof(T).Equals(typeof(RSAParameters)))
                await Task.Run(() =>
                {
                    while (!RecivedKey) ;
                }).ConfigureAwait(false);
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
                    try
                    {
                        byte[] b = ObjectContainer.Encapsulate(bytes, typeof(T).Name);

                        t = Connection.SendAsync(b);
                    }
                    catch (SocketException ex)
                    {
                        DisconnectedFrom(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                    }
                }
            if (t != null)
                await t.ConfigureAwait(false);
        }

        public void SendFile(string path, string name = null)
        {
            while (GlobalDefaults.UseEncryption && !RecivedKey) ;

            name = name != null ? name + Path.GetExtension(path) : Path.GetFileName(path);

            int size = 2048;

            using (FileStream fs = File.OpenRead(path))
            {
                byte[] bytes = new byte[size];
                int bit;
                int i = 0;
                while ((bit = fs.ReadByte()) != -1 && IsConnected)
                {
                    if (i == size)
                    {
                        i = 0;
                        SendFileSegment(bytes, name, fs.Length);
                        bytes = new byte[size];
                    }
                    bytes[i] = (byte)bit;
                    i++;

                    if (!IsConnected) return;
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
            await Task.Run(() =>
            {
                while (GlobalDefaults.UseEncryption && !RecivedKey) ;
            }).ConfigureAwait(false);

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
                        await SendFileSegmentAsync(bytes, name, fs.Length).ConfigureAwait(false);
                        bytes = new byte[size];
                    }
                    bytes[i] = (byte)bit;
                    i++;

                    if (!IsConnected) return;
                }
                byte[] lastBytes = new byte[i];
                for (int j = 0; j < i; j++)
                {
                    lastBytes[j] = bytes[j];
                }
                await SendFileSegmentAsync(bytes, name, fs.Length).ConfigureAwait(false);
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
                try
                {
                    Connection.Send(ObjectContainer.Encapsulate(bytes, name, length));
                }
                catch (SocketException ex)
                {
                    DisconnectedFrom(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                }
            }
        }

        private async Task SendFileSegmentAsync(byte[] bytes, string name, long length)
        {
            Task t = null;
            lock (LockObject)
            {
                if (GlobalDefaults.UseEncryption)
                {
                    bytes = CryptoServices.EncryptAES(bytes, Key);
                }
                try
                {
                    bytes = ObjectContainer.Encapsulate(bytes, name, length);

                    t = Connection.SendAsync(bytes);
                }
                catch (SocketException ex)
                {
                    DisconnectedFrom(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                }
            }
            if (t != null)
                await t.ConfigureAwait(false);
        }

        public T PullObject<T>()
        {
            while (GlobalDefaults.UseEncryption && !RecivedKey) ;

            lock (LockObject)
            {
                for (int i = 0; i < ObjectQueue.Count; i++)
                {
                    if (Utilities.IsHerritableType<T>(ObjectQueue[i].GetType()))
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
            return await t.ConfigureAwait(false);
        }

        public T WaitForPullObject<T>()
        {
            while (IsConnected)
            {
                while (GlobalDefaults.UseEncryption && !RecivedKey) ;
                lock (LockObject)
                    for (int i = 0; i < ObjectQueue.Count; i++)
                    {
                        if (Utilities.IsHerritableType<T>(ObjectQueue[i].GetType()))
                        {
                            object o = ObjectQueue[i];

                            ObjectQueue.RemoveAt(i);

                            return (T)o;
                        }
                    }
                Thread.Sleep(UpdateWaitTime);
            }

            return PullObject<T>();
        }

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

        public object[] GetQueueObjectsTypeless(bool clear = false)
        {
            object[] arr;
            lock (LockObject)
            {
                arr = ObjectQueue.ToArray();
            }
            if (clear) ClearQueue();

            return arr;
        }

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
        private IEnumerator<object> RecieveCoroutine()
        {
            while (IsConnected)
            {
                List<ObjectContainer> objects = new List<ObjectContainer>();

                byte[] Temp = null;

                while (IsConnected)
                {
                    bool br = false;
                    while (Connection.Available == 0 && IsConnected) 
                    {
                        if ((Connection.Poll(1000, SelectMode.SelectRead) && (Connection.Available == 0)) || !Connection.Connected)
                        {
                            DisconnectedFrom(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                            br = true;
                        }
                        if (br) break;
                        yield return null; 
                    }
                    if (br) break;

                    byte[] Buffer = null;

                    try
                    {
                        Buffer = new byte[Connection.Available];

                        Connection.Receive(Buffer);
                    }
                    catch (SocketException ex)
                    {
                        DisconnectedFrom(new DisconnectionContext { type = DisconnectionContext.DisconnectionType.FORCIBLE });
                        break;
                    }

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
                    Type type = item.Type != null ? Utilities.GetTypeFromName(item.Type) : null;

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

                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            bool exists = File.Exists($@"{dir}\{item.fileInfo.Name}");

                            lock(GlobalDefaults.FileLock)
                                using (FileStream fs = exists ?
                                    new FileStream($@"{dir}\{item.fileInfo.Name}", FileMode.Append) :
                                    File.Create($@"{dir}\{item.fileInfo.Name}"))
                                {
                                    fs.Write(content, 0, content.Length);
                                    fs.Flush();
                                    length = fs.Length;
                                }

                            if (length >= item.fileInfo.Length)
                            {
                                using (FileStream fs = new FileStream($@"{dir}\{item.fileInfo.Name}", FileMode.Open))
                                {
                                    OnFileRecieve?.Invoke(new SimpleFile(fs));
                                }
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
                                    if (OnRecieveObject == null) ObjectQueue.Add(ObjectParser.BytesToObject(content, type));
                                    else OnRecieveObject.Invoke(ObjectParser.BytesToObject(content, type));
                                }
                            }
                        }
                    }
                }
                yield return null;
            }
        }

        private void ManagementLoop()
        {
            while (IsConnected)
            {
                Reciever.MoveNext();
            }
            Reciever.Dispose();
        }

        internal void Manage()
        {
            if (IsConnected)
                Reciever.MoveNext();
            else 
                Reciever.Dispose();
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
                Task.Run(() => OnDisconnect?.Invoke(DisconnectionMode, connectionInfo));
            }
            catch
            {

            }

            IsConnected = false;
            Connection?.Close();

            if (remove)
            {
                ObjectQueue?.Clear();
            }
        }
    }
}