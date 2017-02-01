using System;
using System.Threading.Tasks;
using System.Text;
using System.Net.Sockets;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.ServerIdentifier;
using Pomelo.Net.Pomelium.Server.Semaphore;

namespace Pomelo.Net.Pomelium.Server.Node
{
    public class Node : INode
    {
        private TcpClient _tcpClient;
        private IServerIdentifier _serverIdentifier;
        private ISemaphoreProvider _semaphoreProvider;
        private NodeInfo _nodeInfo;

        public Node(NodeInfo nodeInfo, IServerIdentifier serverIdentifier, ISemaphoreProvider semaphoreProvider)
        {
            _nodeInfo = nodeInfo;
            _tcpClient = new TcpClient();
            _serverIdentifier = serverIdentifier;
            _semaphoreProvider = semaphoreProvider;
            TryConnectAsync(_nodeInfo);
        }

        public NodeInfo NodeInfo => _nodeInfo;

        protected virtual async Task TryConnectAsync(NodeInfo nodeInfo)
        {
            foreach(var x in nodeInfo.AddressList)
            {
                try
                {
                    await _tcpClient.ConnectAsync(x, nodeInfo.Port);
                    var stream = _tcpClient.GetStream();
                    HandleStream(stream);
                    break;
                }
                catch { }
            }
        }

        protected async Task HandleStream(NetworkStream stream)
        {
            while (true)
            {
                var buffer = new byte[4];
                await stream.ReadAsync(buffer, 0, 4);
                var length = BitConverter.ToInt32(buffer, 0);
                buffer = new byte[length];
                await stream.ReadAsync(buffer, 0, length);
                var jsonStr = Encoding.UTF8.GetString(buffer);
                var packet = JsonConvert.DeserializeObject<Packet>(jsonStr);
                await HandlePacket(packet);
            }
        }

        protected virtual async Task HandlePacket(Packet body)
        {
            if (body.Type == PacketType.ForwardBack)
            {
                _semaphoreProvider.SetResult(body.RequestId, body.ReturnValue);
            }
            else if (body.Type == PacketType.Exception)
            {
                _semaphoreProvider.SetException(body.RequestId, new PomeliumException(body.ReturnValue.ToString()));
            }
        }


        public virtual async Task<object> InvokeAsync(Guid sessionId, string method, object[] args)
        {
            var requestId = _semaphoreProvider.Create();
            await _tcpClient.SendAsync(new Packet
            {
                SessionId = _serverIdentifier.GetIdentifier(),
                Type = PacketType.Forward,
                RequestId = requestId,
                Arguments = new object[] 
                {
                    new Packet
                    {
                        Type = PacketType.Request,
                        SessionId = sessionId,
                        Method = method,
                        Arguments = args,
                        RequestId = Guid.NewGuid()
                    }
                }
            });
            return _semaphoreProvider.GetTaskById(requestId);
        }

        ~Node()
        {
            try
            {
                _tcpClient.Send(new Packet
                {
                    SessionId = _serverIdentifier.GetIdentifier(),
                    Type = PacketType.NodeDisconnect
                });
            }
            catch { }
        }
    }
}
