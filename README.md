# simple-network-library
Simple net is a .NET standard library that greatly simplifies the process of networking. 

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
