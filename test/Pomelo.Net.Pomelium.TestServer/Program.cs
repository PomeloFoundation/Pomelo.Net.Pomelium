using System;
using Pomelo.Net.Pomelium.Server;

namespace Pomelo.Net.Pomelium.TestServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var server = new PomeliumServer();
            server.Start("127.0.0.1", 6000);
            Console.Read();
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
