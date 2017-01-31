using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.Session;

namespace Pomelo.Net.Pomelium.Server
{
    public class PomeliumServer
    {
        private TcpListener _tcpListner;
        private Dictionary<Guid, PomeliumClientOnServerSide> _clients = new Dictionary<Guid, PomeliumClientOnServerSide>();
        private Dictionary<Guid, TaskCompletionSource<object>> _remoteTaskSemaphore = new Dictionary<Guid, TaskCompletionSource<object>>();
        private HashSet<Type> _hubs = new HashSet<Type>();
        private IPomeliumHubLocator _pomeliumHubLocator;
        private ISession _session;
        private IServiceProvider _serviceProvider;

        public dynamic Client(Guid id) => _clients[id];
        public dynamic Client(string id) => _clients[Guid.Parse(id)];

        public PomeliumServer(IPomeliumHubLocator pomeliumHubLocator = null, ISession session = null, IServiceProvider serviceProvider = null)
        {
            _pomeliumHubLocator = pomeliumHubLocator ?? new DefaultPomeliumHubLocator();
            _session = session ?? new MemorySession();
            _serviceProvider = serviceProvider;
        }

        public async void Start(string address, int port)
        {
            var ip = await Dns.GetHostAddressesAsync(address);
            var endpoint = new IPEndPoint(ip.First(), port);
            Start(endpoint);
        }

        public async void Start(IPEndPoint endpoint)
        {
            foreach (var x in (new DefaultPomeliumHubLocator()).GetHubs())
            {
                _hubs.Add(x);
            }
            _tcpListner = new TcpListener(endpoint);
            _tcpListner.Start();
            while(true)
            {
                HandleClient(new PomeliumClientOnServerSide(await _tcpListner.AcceptTcpClientAsync(), this));
            }
        }

        private async Task HandleClient(PomeliumClientOnServerSide client)
        {
            var stream = client.TcpClient.GetStream();
            while(true)
            {
                var buffer = new byte[4];
                await stream.ReadAsync(buffer, 0, 4);
                var length = BitConverter.ToInt32(buffer, 0);
                buffer = new byte[length];
                await stream.ReadAsync(buffer, 0, length);
                var jsonStr = Encoding.UTF8.GetString(buffer);
                var packet = JsonConvert.DeserializeObject<PacketBody>(jsonStr);
                await HandlePacket(packet, client);
            }
        }

        protected virtual async Task HandlePacket(PacketBody body, PomeliumClientOnServerSide sender)
        {
            if(body.SessionId == null || body.SessionId.Value == default(Guid))
            {
                var id = Guid.NewGuid();
                _clients.Add(id, sender);
                body.SessionId = id;
                await ResponseAsync(sender, new PacketBody
                {
                    ReturnValue = id,
                    Type = PacketType.InitSession
                });
            }
            if (body.Type == PacketType.InitSession)
            {
                _clients.Add(Guid.Parse(body.ReturnValue.ToString()), sender);
            }
            else if (body.Type == PacketType.Exception)
            {
                if (_remoteTaskSemaphore.ContainsKey(body.RequestId))
                {
                    var tcs = _remoteTaskSemaphore[body.RequestId];
                    _remoteTaskSemaphore.Remove(body.RequestId);
                    tcs.SetException(new PomeliumException(body.ReturnValue.ToString()));
                }
            }
            else if (body.Type == PacketType.Response)
            {
                if (_remoteTaskSemaphore.ContainsKey(body.RequestId))
                {
                    var tcs = _remoteTaskSemaphore[body.RequestId];
                    _remoteTaskSemaphore.Remove(body.RequestId);
                    tcs.SetResult(body.ReturnValue);
                }
            }
            else
            {
                var hub = _hubs.FirstOrDefault(x => x.Name == body.Hub + "Hub" || x.Name == body.Hub);
                if (hub == null)
                {
                    await ResponseAsync(sender, new PacketBody
                    {
                        RequestId = body.RequestId,
                        Type = PacketType.Response,
                        ReturnValue = null
                    });
                }
                else
                {
                    var hubInstance = (PomeliumHub)Activator.CreateInstance(hub);
                    var ctx = new PomeliumContext
                    {
                        Client = sender,
                        Request = body,
                        Session = body.SessionId.HasValue ? new SessionCollection(_session, body.SessionId.Value) : null,
                        Resolver = _serviceProvider,
                        SessionId = body.SessionId.Value
                    };
                    hubInstance.Context = ctx;
                    var method = hub.GetTypeInfo().GetMethod(body.Method);
                    dynamic ret;
                    try
                    {
                        var parameters = method.GetParameters();
                        var args = new List<object>();
                        for (var i = 0; i < parameters.Count(); i++)
                        {
                            args.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(body.Arguments[i]), parameters[i].ParameterType));
                        }
                        ret = method.Invoke(hubInstance, args.ToArray());
                    }
                    catch(Exception ex)
                    {
                        await ResponseAsync(sender, new PacketBody
                        {
                            Type = PacketType.Exception,
                            ReturnValue = ex.ToString(),
                            RequestId = body.RequestId
                        });
                        return;
                    }
                    if (method.ReturnType == typeof(void))
                    {
                        await ResponseAsync(sender, new PacketBody
                        {
                            RequestId = body.RequestId,
                            Type = PacketType.Response,
                            ReturnValue = null
                        });
                        return;
                    }
                    try
                    {
                        if (method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                        {
                            try
                            {
                                ret = await ret;
                            }
                            catch (Exception ex)
                            {
                                await ResponseAsync(sender, new PacketBody
                                {
                                    Type = PacketType.Exception,
                                    ReturnValue = ex.ToString(),
                                    RequestId = body.RequestId
                                });
                                return;
                            }
                            await ResponseAsync(sender, new PacketBody
                            {
                                RequestId = body.RequestId,
                                Type = PacketType.Response,
                                ReturnValue = ret
                            });
                        }
                    }
                    catch
                    {
                        if (method.ReturnType == typeof(Task))
                        {
                            try
                            {
                                await ret;
                            }
                            catch (Exception ex)
                            {
                                await ResponseAsync(sender, new PacketBody
                                {
                                    Type = PacketType.Exception,
                                    ReturnValue = ex.ToString(),
                                    RequestId = body.RequestId
                                });
                                return;
                            }
                            await ResponseAsync(sender, new PacketBody
                            {
                                RequestId = body.RequestId,
                                Type = PacketType.Response,
                                ReturnValue = null
                            });
                        }
                        else
                        {
                            await ResponseAsync(sender, new PacketBody
                            {
                                RequestId = body.RequestId,
                                Type = PacketType.Response,
                                ReturnValue = ret
                            });
                        }
                    }
                }
            }
        }

        public static async Task ResponseAsync(PomeliumClientOnServerSide client, PacketBody body)
        {
            await client.TcpClient.SendAsync(body);
        }
    }
}
