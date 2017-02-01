using System;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.Net.Pomelium.Server;

namespace Pomelo.Net.Pomelium.TestServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDistributedMemoryCache();
            serviceCollection.AddPomeliumServer();
            var services = serviceCollection.BuildServiceProvider();
            var server = services.GetRequiredService<PomeliumServer>();
            server.OnConnectedEvents += Server_OnConnectedEvents;
            server.OnDisconnectedEvents += Server_OnDisconnectedEvents;
            server.Start("127.0.0.1", 6000);
            Console.Read();
        }

        private static void Server_OnDisconnectedEvents(Server.Client.LocalClient obj)
        {
            Console.WriteLine(obj.SessionId + " Disconnected");
        }

        private static void Server_OnConnectedEvents(Server.Client.LocalClient obj)
        {
            Console.WriteLine(obj.SessionId + " Connected");
        }
    }

    public class TestHub : PomeliumHub
    {
        public int TestMethod(int a, int b)
        {
            return a + b;
        }
    }
}
