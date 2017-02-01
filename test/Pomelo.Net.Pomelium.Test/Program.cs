using System;
using System.Threading.Tasks;
using Pomelo.Net.Pomelium.Client;

namespace Pomelo.Net.Pomelium.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Test().Wait();
            Console.Read();
        }

        public static async Task Test()
        {
            var client = new PomeliumClient();
            await client.ConnectAsync("127.0.0.1", 6000);
            var ret = await client.Server["TestHub"].TestMethod(1, 1);
            Console.WriteLine(ret);
            client = null;
            GC.Collect();
        }
    }
}
