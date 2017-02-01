using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.Client;
using Pomelo.Net.Pomelium.Server.Session;
using Pomelo.Net.Pomelium.Server.Semaphore;
using Pomelo.Net.Pomelium.Server.HubActivitor;
using Pomelo.Net.Pomelium.Server.Node;

namespace Pomelo.Net.Pomelium.Server
{
    public class PomeliumServer
    {
        private TcpListener _tcpListner;
        private ISemaphoreProvider _semaphoreProvider;
        private IPomeliumHubLocator _pomeliumHubLocator;
        private ISession _session;
        private IServiceProvider _serviceProvider;
        private IHubActivitor _hubActivitor;
        private IClientCollection _clientCollection;
        private INodeProvider _nodeProvider;

        public dynamic Client(Guid id)
        {
            var task = _clientCollection.GetClientAsync(id);
            task.Wait();
            return task.Result;
        }

        public dynamic Client(string id) => Client(Guid.Parse(id));

        public PomeliumServer(
            IPomeliumHubLocator pomeliumHubLocator,
            ISession session,
            IServiceProvider serviceProvider,
            ISemaphoreProvider semaphoreProvider,
            IHubActivitor hubActivitor,
            IClientCollection clientCollection,
            INodeProvider nodeProvider)
        {
            _pomeliumHubLocator = pomeliumHubLocator;
            _session = session;
            _serviceProvider = serviceProvider;
            _semaphoreProvider = semaphoreProvider;
            _hubActivitor = hubActivitor;
            _clientCollection = clientCollection;
            _nodeProvider = nodeProvider;
        }

        public async void Start(string address, int port)
        {
            var ip = await Dns.GetHostAddressesAsync(address);
            var endpoint = new IPEndPoint(ip.First(), port);
            Start(endpoint);
        }

        public async void Start(IPEndPoint endpoint)
        {
            _tcpListner = new TcpListener(endpoint);
            _tcpListner.Start();
            while (true)
            {
                var tcpClient = await _tcpListner.AcceptTcpClientAsync();
                var stream = tcpClient.GetStream();
                HandleClient(stream, new LocalClient(tcpClient, _semaphoreProvider));
            }
        }

        protected virtual async Task HandleClient(NetworkStream stream, LocalClient client)
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
                await HandlePacket(packet, client);
            }
        }

        protected virtual async Task HandlePacket(Packet body, LocalClient sender)
        {
            if (body.SessionId == null || body.SessionId.Value == default(Guid))
            {
                var id = Guid.NewGuid();
                sender.SessionId = id;
                _clientCollection.StoreLocalClientAsync(sender);
                body.SessionId = id;
                sender.SessionId = id;
                await ResponseAsync(sender, new Packet
                {
                    ReturnValue = id,
                    Type = PacketType.InitSession
                });
                await OnConnected(sender);
            }
            if (body.Type == PacketType.Forward)
            {
                if (CheckNode(body.SessionId.Value))
                {
                    var packet = JsonConvert.DeserializeObject<Packet>(body.Arguments.First().ToString());
                    object returnValue = null;
                    var client = await _clientCollection.GetClientAsync(packet.SessionId.Value);
                    try { returnValue = await client.InvokeAsync(packet.Method, packet.Arguments); } catch (Exception ex) { await ResponseAsync(sender, new Packet { Code = 403, Type = PacketType.Exception, ReturnValue = ex.ToString() }); }
                    await ResponseAsync(sender, new Packet
                    {
                        Type = PacketType.ForwardBack,
                        RequestId = packet.RequestId,
                        ReturnValue = returnValue
                    });
                }
                else
                {
                    await ResponseAsync(sender, new Packet { Code = 403, Type = PacketType.Exception, ReturnValue = "Forbidden" });
                }
            }
            else if (body.Type == PacketType.InitSession)
            {
                sender.SessionId = Guid.Parse(body.ReturnValue.ToString());
                if (_clientCollection.LocalClientExist(sender.SessionId))
                {
                    _clientCollection.StoreLocalClientAsync(sender);
                    await OnConnected(sender);
                }
            }
            else if (body.Type == PacketType.Disconnect)
            {
                await OnDisconnected(sender);
            }
            else if (body.Type == PacketType.Exception)
            {
                _semaphoreProvider.SetException(body.RequestId, new PomeliumException(body.ReturnValue.ToString()));
            }
            else if (body.Type == PacketType.Response)
            {
                _semaphoreProvider.SetResult(body.RequestId, body.ReturnValue);
            }
            else
            {
                try
                {
                    var hub = _hubActivitor.CreateInstance(body.Hub, sender, body);
                    var method = _hubActivitor.GetMethod(hub, body.Method);
                    dynamic ret = await _hubActivitor.InvokeAsync(hub, method, body.Arguments);
                    await ResponseAsync(sender, new Packet
                    {
                        RequestId = body.RequestId,
                        Type = PacketType.Response,
                        ReturnValue = ret
                    });
                }
                catch (Exception ex)
                {
                    await ResponseAsync(sender, new Packet
                    {
                        Type = PacketType.Exception,
                        ReturnValue = ex.ToString(),
                        RequestId = body.RequestId
                    });
                }
            }
        }

        protected virtual async Task OnConnected(LocalClient Client)
        {

        }

        protected virtual async Task OnDisconnected(LocalClient Client)
        {
            await _clientCollection.RemoveLocalClientAsync(Client);
        }

        protected virtual bool CheckNode(Guid serverId)
        {
            return _nodeProvider.Nodes.Any(x => x.NodeInfo.ServerId == serverId);
        }

        public async Task ResponseAsync(IClient client, Packet body)
        {
            try
            {
                await client.SendAsync(body);
            }
            catch (Exception ex)
            {
                if (ex is SocketException && client is LocalClient)
                {
                    await OnDisconnected(client as LocalClient);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
