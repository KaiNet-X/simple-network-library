# simple-network-library
SimpleNetwork is a .NET standard library that greatly simplifies the process of networking. 

Simple net allows you to start and initialize a flexible tcp server in as little as two lines. 
It contains two main classes, client and server, each that are easy to get connected and sending information between them.

You can send objects to the Server using the SendObject method.
The client class automatically recieves information sent by the remote device. It is then stored in a private object queue.
You can acess this information using the generic PullObject and WaitForPullObject.
Both methods check the object queue for the specified object type if it is found, it is removed from the queue in a first-in-first-out fasion.
If Pullobject does not find it, it returns default.
WaitForPullObject loops until the object is queued, and then returns it.
The generic method HasObjectOfType returns true if the queue has the object, otherwise returns false.
The disconnection context class tells the remote device what to do on disconnect.
If it is set to close connection, the connection is disposed of and the queue is left intact to get the last information from.
If the context is set to remove, the connection is disposed of and the client is cleared.
If the context is set to forcible, meaning the clients disconnected ungracefully, the client uses the default behavior for forcible disconnection in the global defaults class.
There is an OnDisconnect event that sends a DisconnectionContext as well as a Connectioninfo class, which is called when the remote device disconnects from it.

The server class has a client cap that can be set in the constructor. 
When clients connect to it, they are added to a private list of clients. The user can send objects to them using the SendToAll method, or the SendToClient method that requires an index. 
To recieve information, there is a method called PullFromClient that takes an index parameter.
There is an OnConnect as well as an OnDisconnect event that is called whenever a client connects or disconnects.

## Getting started
So, how do you actually send data?
No kidding, its as simple as
~~~
Server S = new Server(IPAddress.Loopback, 5454, 1);
S.StartServer()

Client c = new Client();
c.Connect(IPAddress.Loopback, 5454);

s.SendToAll([Any object]);

[Any object] obj = c.WaitForPullObject<T>();
~~~
[Any object] is anyt object type of your chosing. Yes, you no longer have to create some confusing format for sending over sockets and implement it on every single object, you can simply send it and it works.

Now, if you want to send from the client, simply replace
~~~
s.SendToAll(obj);

[Any object] obj = c.WaitForPullObject<[Any object]>();
~~~
with
~~~
c1.SendObject(Test1);

[Any object] o1 = S.WaitForPullFromClient<[Any object]>(0);
~~~
## Sending between applications
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

            Console.WriteLine(S.WaitForPullFromClient<string>(1));

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

            C.SendObject(Console.ReadLine());

            Console.WriteLine(C.WaitForPullObject<string>());

            C.Clear();
        }
    }
}
~~~
Now that you have a basic understanding on how this library works, on to the documentation!

# Docs

##### Classes
* Server
-- Manages multiple client connections. Has methods for sending to clients, recieving from clients, and events that allow you to track new connections and disconnections.
* Client
-- Connects to a server, sends and recieves data with server.
* ConnectionInfo
-- Information about a given connection such as IP Address and port number.
* DisconnectionContext
-- Class sent over sockets when disconnect is called. Manages what the remote device does when disconnected from.
* GlobalDefaults
-- Static class that manages the default settings used by SimpleNetwork, such as wheather to use JSON or MESSAGEPACK or how an ungraceful disconnect is handled.
* ClientModel
-- Models a client and has a good amount of a specified client's information. Used to get information about a client from the server.
* ClientAccessor
-- Class that manages getting of ClientModel by index.

##### Server properties

* int UpdateClientWaitTime 
-- Get or set the time in milliseconds the client waits in between recieve calls.
* ushort MaxClients
-- Gets the maximum amount of clients the server can manage at a time.
* ushort ClientCount
-- Gets the amount of clients that the server is currently managing. This includes clients that have disconnected, but are set to stay.
* bool Running
--Gets the boolean value of the server running.
* public bool RestartAutomatically
-- Gets or sets the boolean value for the server to automatically start accepting clients after one has been disconnected and removed.
* ReadonlyClients
-- ClientAccessor object that returns ClientModel object based on client index.

##### Server events
* OnClientDisconnect
-- Invoked whenever a client disconnects, passes a DisconnectionContext object as well as a ConnectionInfo object to it's subscribers.
* OnClientConnect
-- Invoked whenever a client connects, passes a ConnectionInfo object to it's subscribers.

##### Server methods
* StartServer()
-- Starts accepting connections on the server.
* StopServer()
-- Manually stops server from accepting connections.
* SendToAll<T>(T obj)
-- Sends an object to all clients on the server.
* Close()
-- Stops the server, disconnects all clients and removes them. Essentially terminates the server.
* SendToOne<T>(T obj, ushort index)
-- Sends an object to a client at the specified index.
* bool ClientHasObjectType<T>(ushort index)
-- Checks if client at index has a certain object type.
* T PullFromClient<T>(ushort index)
-- Tries to pull an object from client at specified index. If the client does not have that specified object type,returns default.
* T WaitForPullFromClient<T>(ushort index)
-- Waits until the client has the specified object type. Once it does, returns the object.
* ClearDisconnectedClients()
-- Removes all of the clients that have been disconnected from.
* DisconnectClient(ushort index, bool remove = false)
--Disconnects a client at a givin index. If remove is set to true, the client gets removed. Otherwise, the client is kept until it's queue is empty.
* DisconnectClient(ushort index, DisconnectionContext ctx, bool remove = false)
-- Overload of DisconnectClient that allows you to specify DisconnectionContext.
* DisconnectAllClients(bool remove = false)
-- Disconnects all of the clients. If set to remove, removes the clients as well.
* DisconnectAllClients(DisconnectionContext ctx, bool remove = false)
-- Overload of DisconnectAllClients that allows user to specify disconnection context.

##### Server constructors
* Server(IPAddress iPAddress, int PortNum, ushort MaxClients)
-- Creates a server that listens on iPAddress to port PortNum where MaxClients can connect to it.
* Server(string iPAddress, int PortNum, ushort MaxClients)
-- Same as other constructor, only uses string address and throws an exception if invalid.
##### Client properties

* int UpdateWaitTime 
-- Get or set the time in milliseconds the client waits in between recieve calls.
* int QueuedObjectCount
-- Gets the amount of ovjects in the object's queue.
* ConnectionInfo connectionInfo
-- Returns the client's connection info.
* bool Running
-- Checks if the management loop is executing
* bool IsConnected
-- Checks if the client is connected

##### Client Events
* OnDisconnect
-- Invoked whenever a client disconnects. Passes DisconnectionContext and ConnectionInfo to subscribers.
* OnConnect
-- Invoked whenever BeginConnect finishes. Passes ConnectionInfo to subscribers.

##### Client Methods

* Connect(IPAddress address, int port)
-- Connects to server hosted on address and port
* Connect(String address, int port)
-- Overload of connect allows you to pass a string for address, throws exception if invalid.
* BeginConnect(IPAddress address, int port)
-- Begins connecting to server asynchronously. Invokes OnConnect.
* BeginConnect(string address, int port)
-- Overload of BeginConnect that uses a string as address, throws an exception if invalid.
* CancelConnect()
-- Cancels BeginConnect if not already connected.
* Disconnect()
-- Disconnects from the server.
* Disconnect(DisconnectionContext ctx)
-- Overload of Disconnect that allows the user to pass DisconnectionContext.
* Clear()
-- Disconnects from server and clears the queue.
* SendObject<T>(T obj)
-- Sends an object to the server.
* T PullObject<T>()
-- Tries to pull an object from client. If the client does not have that specified object type, returns default.
* T WaitForPullObject<T>()
-- Waits until the client has specified object type and returns the first occurence of it.
* bool HasObjectType<T>()
-- Checks if client has specified object type.

##### ConnectionInfo properties

* IPAddress LocalAddress
-- Address of local machine.
* string LocalHostName
-- Host name of local macine.
* IPAddress RemotelAddress
-- Address of remote machine.
* string RemoteHostName
-- Hostname of remote machine.

##### DisconnectionContext.DisconnectionType (enum)

* CLOSE_CONNECTION
-- Closes connection but doesn't remove/clear it
* REMOVE
-- Closes connection and removes/clears it
* FORCIBLE
-- When the connection is ungraceful, GlobalDefaults.ForcibleDisconnectMode determines what happens.

##### DisconnectionContext properties
* DisconnectionType type
-- disconnection type

##### GlobalDefaults
Static class that manages several default values. If you want to use different defaults, set them before you run any other simplenet code. If you change them in the middle of normal opperations, systems will not work correctly.

##### GlobalDefaults.ForcibleDisconnectBehavior (enum)
* REMOVE
-- When there is a forcible disconnection, clears client.
* KEEP
-- When there is a forcible disconnection, keeps client.

##### GlobalDefaults.EncodingType
* JSON
-- Slower encoding that takes up more memory, but more classes work with it.
* MESSAGE_PACK
-- Faster and more compact encoding that 

##### GlobalDefaults.RunServerClientsOnOneThread(bool)
When connecting a massive amount of clients, set to true in order to use one thread for all of the clients server-side. Does not affect client normaly.