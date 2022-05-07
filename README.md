# NOTE
The successor to this package targeting dotnet 6 has been released

[Repo](https://github.com/KaiNet-X/Network)

[NuGet](https://www.nuget.org/packages/KaiNet.Net/1.0.0)

# Simple network
SimpleNetwork is a .NET standard library that is compatible with most poject types that greatly simplifies the process of networking. 

Simple net allows you to start and initialize a flexible tcp server in as little as two lines. It contains two main classes, client and server, each that are easy to get connected and sending information between them.

The Client object manages data it recieves converting it back to the original type it was sent as, and has many different methods of accessing said data. It\'s send methods are generic, allwoing the user to send any object without effort.

The Server object manages client connections, storing clients in a list. It has methods for working with clients and allows sending to one client or all clients. It allows the user the same flexibility of sending and recievign data, only it is on the server side.

These classes are all you need to get started, but if you want more granular control and managemet of the system, there is the static GlobalDefaults class which allows you to configure certain behaviors, DisconnectionContext class which determines the remote endpoint\'s behavior when disconnected from that you can view, and the ConnectionInfo class which has the IP address and name of remote endpoints.

## Getting started
So, how do you actually send data?
No kidding, its as simple as
~~~
Server S = new Server(IPAddress.Loopback, 5454, 1);
S.StartServer()

Client c = new Client();
c.Connect(IPAddress.Loopback, 5454);

s.SendToAll([object T]);

T obj = c.WaitForPullObject<T>();
~~~
T is any object type of your chosing. Yes, you no longer have to create some confusing format for sending over sockets and implement it on every single object, you can simply send it and it works.

Now, if you want to send from the client, simply replace
~~~
s.SendToAll(obj);

T obj = c.WaitForPullObject<T>();
~~~
with
~~~
c.SendObject(Test1);

T o1 = S.WaitForPullFromClient<T>(0);
~~~
## Basic data transfer between applications
Create your server application
~~~
using System;
using System.Net;
using SimpleNetwork;

namespace ServerAppilcation
{
    class Program
    {
        static void Main(string[] args)
        {
            Server S = new Server(IPAddress.Loopback, 5454, 1);
            S.StartServer();

			/*
			** waiting for string to be recieved on the first client
			** and print to console
			*/
            Console.WriteLine(S.WaitForPullFromClient<string>(0));

            S.SendToAll("Goodbye");

            S.Close();
        }
    }
}
~~~
And your client application
~~~
using System;
using SimpleNetwork;
using System.Net;

namespace ClientApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            Client C = new Client();
            C.Connect(IPAddress.Parse("192.168.0.17"), 5454);
			
			// sends user input
            C.SendObject(Console.ReadLine());

            Console.WriteLine(C.WaitForPullObject<string>());

            C.Clear();
        }
    }
}
~~~
Now that you have a basic understanding on how this library works, on to the extensive documentation!

# Docs
### Server

-- Manages multiple client connections. Has methods for sending to clients, recieving from clients, and events that allow you to track new connections and disconnections.

##### Constructors
~~~
new Server(IPAddress iPAddress, int PortNum, ushort MaxClients)
new Server(string iPAddress, int PortNum, ushort MaxClients)
~~~
##### Server management
~~~
// Starts accepting connections. Stops when max clients have been reached 
StartServer()

// Stops accepting connections
StopServer()

// Stops accepting connections, closes and removes existing connections
Close()

// Removes all clients that have disconnected from client list. It's data // is no longer accessible.
ClearDisconnectedClients()
~~~
##### Work with clients
~~~
// Send object to all clients
SendToAll<T>(T obj)

// Send object to client at index
SendToOne<T>(T obj, ushort index)

// Sends file to all clients
SendFileToAll(string path, string name = null)

// Sends file to client at specific index
SendFileToOne(string path, ushort index, string name = null)

// Asyncronous versions of previous methods
Task SendToAllAsync<T>(T obj)
Task SendToOneAsync<T>(T obj, ushort index)
Task SendFileToAllAsync(string path, string name = null)
Task SendFileToOneAsync(string path, ushort index, string name = null)

// Check if client has object of type in in's queue
bool ClientHasObjectType<T>(ushort index)

// If client has valid type, pull the first occurence. If not, return 
// default.
T PullFromClient<T>(ushort index)

// If client does not have type T, wait until it does and then pull the // first occurence.
T WaitForPullFromClient<T>(ushort index)

// Async version of WaitForPullFromClient
Task<T>PullFromClientAsync<T>(ushort index)

// Returns all objects in client queue. If clear, remove them from queue // as well.
object[] GetClientQueueObjectsTypeless(ushort index, bool clear = false)

// Returns all objects in client queue that are the specified type. If
// clear, remove them from queue as well.
T[] GetClientQueueTyped<T>(ushort index, bool clear = false)

// Async versions of previous methods
Task<object[]> GetClientQueueObjectsTypelessAsync(ushort index, bool clear = false)
Task<T[]> GetClientQueueTypedAsync<T>(ushort index, bool clear = false)

// Clears queue of specific client
ClearClientQueue(ushort index)

// Clears queue of all clients
ClearAllQueue()

// Disconencts client with default disconnection context
DisconnectClient(ushort index, bool remove = false)

// Disconnects client with chosen context
DisconnectClient(ushort index, DisconnectionContext ctx, bool remove = false)

// Disconencts all clients. If remove is set to true, their information // can no longer be accessed.
DisconnectAllClients(bool remove = false)

// Disconencts all clients with chosen context. If remove is set to true, their information // can no longer be accessed.
DisconnectAllClients(DisconnectionContext ctx, bool remove = false)
~~~

##### Properties/Fields
~~~
// Gets or sets the time between calls to manage clients. Only applicable // when GlobalDefaults.RunServerClientsOnOneThread == true
int ClientUpdateWaitTime

// Gets the amount of clients the server is managing
ushort ClientCount

// Gets if the server is currently accepting connections
bool Running

// Tells the server to restart automatically when client count drops 
// below max
bool RestartAutomatically

// Indexible class that returns a client model object.
ClientAccessor ReadonlyClients
~~~

##### Delegates/Events
~~~
// Provides the disconnection context and the conenction info of the
// client that disconnected
delegate void ClientDisconnected(DisconnectionContext ctx, ConnectionInfo inf)

// Provides the client info of the client that connected and the index that it can be
// accessed in the server
delegate void ClientConnected(ConnectionInfo inf, ushort index)

// Provides the file and the connection info who recieved the file
delegate void RecievedFile(SimpleFile file, ConnectionInfo info)

// Provides the latest object and the conenctioninfo for the client that recieved it
public delegate void RecievedObject(object obj, ConnectionInfo info)

// Invoked when a client disconnects
event ClientDisconnected OnClientDisconnect

// Invoked when a client connects
event ClientConnected OnClientConnect

// Invoked when a client recieves a file
RecievedFile OnClientRecieveFile

// Invoked when a client recieves an object. If there are any listeners, objects are not
// stored in the queue
public event RecievedObject OnClientRecieveObject
~~~

##### Subclasses
~~~
// Indexible class for accessing ClientModels
Server.ClientAccessor

// Some readonly information about a client
Server.ClientAccessor.ClientModel
	ConnectionInfo Info;
	bool IsConnected;
	int QueuedObjectCount;
~~~

### Client
-- Connects to a server, sends and recieves data with server.

##### Constructor
~~~
new Client();
~~~
##### Work with data

~~~
// Sends a c# object over the network
SendObject<T>(T obj)

// Sends a file over the network. If name is provided, sends the file 
// with the new name.
SendFile(string path, string name = null)

// If there isn't an object of specific type in the queue, return 
// default. Otherwise, return the first occurence of that type.
T PullObject<T>()

// Waits until target type is in queue. once it is, return the first 
// occurence
T WaitForPullObject<T>()

// Check if T object is in the queue
HasObjectType<T>()

// Gets the entire queue. If set to clear, also clears the internal queue
object[] GetQueueObjectsTypeless(bool clear = false)

// Gets all instances of t from the queue. If set to clear, removes them
// from the queue.
T[] GetQueueObjectsTyped<T>(bool clear = false)

// Async versions of previous methods where applicable.
Task SendObjectAsync<T>(T obj)
SendFileAsync(string path, string name = null)
Task<T> PullObjectAsync<T>()
Task<object[]> GetQueueObjectsTypelessAsync(bool clear = false)
Task<T[]> GetQueueObjectsTypedAsync<T>(bool clear = false)
~~~
##### Connection management

~~~
// Connect to address at port. Waits until connection is successful.
Connect(IPAddress address, int port)
Connect(string address, int port)

// Connect to address at port. Starts connection on a new thread
BeginConnect(IPAddress address, int port)
BeginConnect(string address, int port)

// Disconnects from server
Disconnect()

// Disconnects from server, allows user to specify DisconnectionContext
Disconnect(DisconnectionContext ctx)

// Clears the queue and terminates the connection.
Clear()

// Clears the queue
ClearQueue()

// Async connection methods
Task ConnectAsync(IPAddress address, int port)
Task ConnectAsync(string address, int port)
~~~

##### Properties/Fields
~~~
// Time in between recieve calls and beginConnect attempts 
int UpdateWaitTime

// How many objects are in the queue
int QueuedObjectCount

// Client connection information
ConnectionInfo connectionInfo

// Check if client is running
bool Running

// Check if client is connected
bool IsConnected

// If not disconencted, null. otherwise the disconnection context
DisconnectionContext DisconnectionMode
~~~

##### Delegates/Events
~~~
// Provides disconnection context and info
delegate void Disconnected(DisconnectionContext ctx, ConnectionInfo inf)

// Provides file
delegate void RecievedFile(SimpleFile file)

// Provides ConnectionInfo
delegate void Connected(ConnectionInfo inf)

// Shows the exception and amount of attempts the client has tried to connect. 
// If return true, keep trying to connect. Otherwise, stop
delegate bool ConnectException(SocketException ex, uint attempts)

// provides the object that the client has recieved
delegate void RecievedObject(object obj)
		
// Invoked when disconnected from
event Disconnected OnDisconnect

// Invoked when begincoinnect is successful
event Connected OnConnect

// Invoked when client recievs a file
RecievedFile OnFileRecieve

// Invokes when the client recieves an object. If there are any listeners, Objects are
// not stored in queue
event RecievedObject OnRecieveObject

// Invoked when there is a socketexception durring a connect method. Only one listener 
// allowed
ConnectException OnConnectError
~~~

### GlobalDefaults
-- Static class containing several miscelaneous properties, fields, and methods that control connection behaviors

##### Enums
~~~
public enum ForcibleDisconnectBehavior
{
	REMOVE,
	KEEP
}
public enum EncodingType
{
	JSON,
	MESSAGE_PACK
}
~~~

##### Methods
~~~
// clears all files recieved by simple net
ClearSentFiles()
~~~

##### Fields/Properties
~~~
// Determines weather the server will keep or remove a client that was
// forcibly disconnected. By default KEEP
ForcibleDisconnectBehavior ForcibleDisconnectMode

// Choses wheather to use JSON or Message pack as encoding type. Must be
// same for server and client endpoints. By default MESSAGE_PACK
EncodingType ObjectEncodingType

// Determines if the server will manage client operations on one thread
// or if each client gets it's own thread. By default true
bool RunServerClientsOnOneThread

// If true, client will only keep the latest object ofe each type
// recieved. By default false
bool OverwritePreviousOfTypeInQueue

// Determines whether the network uses encryption. Must be the same on
// client and server. By default true
bool UseEncryption

// Use this if you want to use a custom messagepack serializer.
MessagePackSerializerOptions Serializer

// Path where recieved files are stored. By default directory of running
// running project + \SentFiles
string FileDirectory
~~~

### SimpleFile
-- Passed as the first parameter for the recieve file delegates. Wraps a filestream and has methods for easily working with it. Automatically disposed after the function finishes.

##### Properties/Fields
~~~
// Readonly filestream object
public FileStream Stream

// Fully qualified path of the file
public string FullPath

// Name of file
public string Name

// Extension of file
public string Extension
~~~

##### Methods
~~~
// deletes the file
Delete()

// Copies the file to specified path. Optionally sets its name and overwrites file with
// same name and extension.
CopyToPath(string NewPath, string Name = null, bool OverwriteFile = false)

// Same as CopyToPath, only deletes the file at the end.
MoveToPath(string NewPath, string Name = null, bool OverwriteFile = false)

// Releases resources
Dispose()
~~~

### DisconnectionContext
-- Sent whenever disconnected, informs the endpoint of disconnection type

##### Enums
~~~
public enum DisconnectionType
{
	CLOSE_CONNECTION,
	REMOVE,
	FORCIBLE
}
~~~

##### Properties/Fields
~~~
// Specifies if its simply closing the conenction, removing the client or
// if it was disconnected by outside circumstances
DisconnectionType type
~~~

### ConnectionInfo
-- Has information about a connection

##### Properties/Fields
~~~
// Local IP Address
IPAddress LocalAddress

// Local host name
string LocalHostName

// Remote IP Address
IPAddress RemoteAddress

// Remote host name
string RemoteHostName

// Readonly identifyer for clients (use this for keeping track of clients server side)
Guid ID
~~~
