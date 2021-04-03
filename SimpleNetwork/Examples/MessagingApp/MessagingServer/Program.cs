using System;
using System.Net;
using System.Threading.Tasks;
using SimpleNetwork;

namespace MessagingServer
{
    class Program
    {
        static Server server = new Server(IPAddress.Any, 12233, 8);

        static void Main(string[] args)
        {
            GlobalDefaults.ObjectEncodingType = GlobalDefaults.EncodingType.JSON;

            server.OnClientConnect += OnConnect;
            server.OnClientDisconnect += OnDisconnect;
            server.StartServer();
            Console.WriteLine("Server running");
            Task.Run(() => Serve()).Wait();
        }

        static async Task Serve()
        {
            while (true)
            {
                for (ushort i = 0; i < server.ClientCount; i++)
                {
                    if (server.ClientHasObjectType<SendMessage>(i))
                    {
                        SendMessage msg = await server.PullFromClientAsync<SendMessage>(i).ConfigureAwait(false);
                        Console.WriteLine($"{msg.Username} ({server.ReadonlyClients[i].Info.RemoteHostName}) sent \"{msg.Content}\" at {msg.Time}");
                        await server.SendToAllAsync(msg).ConfigureAwait(false);
                    }
                }
            }
        }

        private static void OnDisconnect(DisconnectionContext ctx, ConnectionInfo inf)
        {
            Console.WriteLine($"{inf.RemoteHostName} has disconnected");
        }

        private static void OnConnect(ConnectionInfo inf, ushort index)
        {
            Console.WriteLine($"{inf.RemoteHostName} is connected");
        }
    }
}
