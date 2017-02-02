using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.Client;
using Pomelo.Net.Pomelium.Server.Session;
using Pomelo.Net.Pomelium.Server.Semaphore;
using Pomelo.Net.Pomelium.Server.HubActivitor;
using Pomelo.Net.Pomelium.Server.Node;
using Pomelo.Net.Pomelium.Server.GarbageCollector;

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
        private IGarbageCollector _garbageCollector;
        private Timer _heartbeatTimer;
        private bool _heartbeatLocker;

        public event Action<LocalClient> OnConnectedEvents;
        public event Action<LocalClient> OnReconnectedEvents;
        public event Action<LocalClient> OnDisconnectedEvents;

        public static T CreateServer<T>()
            where T : PomeliumServer
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDistributedMemoryCache();
            serviceCollection.AddPomeliumServer<T>();
            var services = serviceCollection.BuildServiceProvider();
            return services.GetRequiredService<T>();
        }

        public static PomeliumServer CreateServer()
        {
            return CreateServer<PomeliumServer>();
        }

        public PomeliumServer(
            IPomeliumHubLocator pomeliumHubLocator,
            ISession session,
            IServiceProvider serviceProvider,
            ISemaphoreProvider semaphoreProvider,
            IHubActivitor hubActivitor,
            IClientCollection clientCollection,
            INodeProvider nodeProvider,
            IGarbageCollector garbageCollector)
        {
            _pomeliumHubLocator = pomeliumHubLocator;
            _session = session;
            _serviceProvider = serviceProvider;
            _semaphoreProvider = semaphoreProvider;
            _hubActivitor = hubActivitor;
            _clientCollection = clientCollection;
            _nodeProvider = nodeProvider;
            _heartbeatTimer = new Timer(TimerCallback, null, 0, 5000);
        }
        public dynamic Client(Guid id)
        {
            var task = _clientCollection.GetClientAsync(id);
            task.Wait();
            return task.Result;
        }

        public dynamic Client(string id) => Client(Guid.Parse(id));

        protected virtual async void TimerCallback(object state)
        {
            if (_heartbeatLocker)
                return;
            _heartbeatLocker = true;
            try
            {
                var tasks = new List<Task>();
                foreach (var x in _clientCollection.GetLocalClients())
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await x.SendAsync(new Packet { SessionId = x.SessionId, Type = PacketType.Heartbeat });
                        }
                        catch
                        {
                            await _clientCollection.RemoveLocalClientAsync(x);
                            OnDisconnected(x);
                            GC.Collect();
                        }
                    }));
                }
                Task.WaitAll(tasks.ToArray());
            }
            finally
            {
                _heartbeatLocker = false;
            }
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
                client.SessionId = packet.SessionId;
                await HandlePacket(packet, client);
            }
        }

        protected virtual async Task HandlePacket(Packet body, LocalClient sender)
        {
            if (!await _session.ExistsAsync(body.SessionId))
            {
                if (body.Type == PacketType.Forward && CheckNode(body.SessionId))
                {
                    sender.IsNode = true;
                }
                else
                {
                    if (await _garbageCollector.IsInBufferAsync(body.SessionId))
                    {
                        await _garbageCollector.UnmarkGarbageAsync(body.SessionId);
                        OnReconnected(sender);
                    }
                    else
                    {
                        OnConnected(sender);
                    }
                    if (_clientCollection.LocalClientExist(body.SessionId))
                        _clientCollection.StoreLocalClientAsync(sender);
                    _session.InitAsync(body.SessionId);
                }
            }

            if (body.Type == PacketType.Forward)
            {
                if (sender.IsNode)
                {
                    var packet = JsonConvert.DeserializeObject<Packet>(body.Arguments.First().ToString());
                    object returnValue = null;
                    var client = await _clientCollection.GetClientAsync(packet.SessionId);
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
            OnConnectedEvents?.Invoke(Client);
        }

        protected virtual async Task OnReconnected(LocalClient Client)
        {
            OnReconnectedEvents?.Invoke(Client);
        }

        protected virtual async Task OnDisconnected(LocalClient Client)
        {
            OnDisconnectedEvents?.Invoke(Client);
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
