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