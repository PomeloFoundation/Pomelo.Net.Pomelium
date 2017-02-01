using System;
using System.Text;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium
{
    public static class TcpClientExtension
    {
        public static void Send(this TcpClient client, Packet packet)
        {
            var stream = client.GetStream();
            var packetBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));
            var lengthBuffer = BitConverter.GetBytes(packetBuffer.Length);
            stream.Write(lengthBuffer, 0, 4);
            stream.Write(packetBuffer, 0, packetBuffer.Length);
        }
        
        public static async Task SendAsync(this TcpClient client, Packet packet)
        {
            var stream = client.GetStream();
            var packetBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(packet));
            var lengthBuffer = BitConverter.GetBytes(packetBuffer.Length);
            await stream.WriteAsync(lengthBuffer, 0, 4);
            await stream.WriteAsync(packetBuffer, 0, packetBuffer.Length);
        }
    }
}
