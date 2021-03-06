﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Text;
using System.Reflection;
using Newtonsoft.Json;

namespace Pomelo.Net.Pomelium.Client
{
    public class PomeliumClient
    {
        private Dictionary<Guid, TaskCompletionSource<object>> _remoteTaskSemaphore = new Dictionary<Guid, TaskCompletionSource<object>>();
        
        public PomeliumClient()
        {
            _tcpClient = new TcpClient();
        }

#if NETSTANDARD1_6
        public Task ConnectAsync(string host, int port)
        {
            var task = _tcpClient.ConnectAsync(host, port);
            HandleStream();
            return task;
        }
#else
        public void Connect(string host, int port)
        {
            _tcpClient.Connect(host, port);
            HandleStream();
        }
#endif

        protected async Task HandleStream()
        {
            var stream = _tcpClient.GetStream();
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
            if (body.Type == PacketType.Exception)
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
                var method = GetType().GetTypeInfo().DeclaredMethods.First(x => x.Name == body.Method && x.IsPublic && x.GetParameters().Count() == body.Arguments.Count());
                dynamic ret;
                try
                {
                    var parameters = method.GetParameters();
                    var args = new List<object>();
                    for (var i = 0; i < parameters.Count(); i++)
                    {
                        args.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(body.Arguments[i]), parameters[i].ParameterType));
                    }
                    ret = method.Invoke(this, args.ToArray());
                }
                catch (Exception ex)
                {
                    await ResponseAsync(new Packet
                    {
                        Type = PacketType.Exception,
                        ReturnValue = ex.ToString(),
                        RequestId = body.RequestId
                    });
                    return;
                }
                if (method.ReturnType == typeof(void))
                {
                    await ResponseAsync(new Packet
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
                            await ResponseAsync(new Packet
                            {
                                Type = PacketType.Exception,
                                ReturnValue = ex.ToString(),
                                RequestId = body.RequestId
                            });
                            return;
                        }
                        await ResponseAsync(new Packet
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
                            await ResponseAsync(new Packet
                            {
                                Type = PacketType.Exception,
                                ReturnValue = ex.ToString(),
                                RequestId = body.RequestId
                            });
                            return;
                        }
                        await ResponseAsync(new Packet
                        {
                            RequestId = body.RequestId,
                            Type = PacketType.Response,
                            ReturnValue = null
                        });
                    }
                    else
                    {
                        await ResponseAsync(new Packet
                        {
                            RequestId = body.RequestId,
                            Type = PacketType.Response,
                            ReturnValue = ret
                        });
                    }
                }
            }
        }

        public async Task ResponseAsync(Packet body)
        {
            var stream = _tcpClient.GetStream();
            var jsonBytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
            var buffer = BitConverter.GetBytes(jsonBytes.Length);
            await stream.WriteAsync(buffer, 0, 4);
            await stream.WriteAsync(jsonBytes, 0, 0);
        }

        private TcpClient _tcpClient;

        public TcpClient TcpClient { get { return _tcpClient; } }

        public Guid SessionId { get; set; }
        
        public dynamic Server { get { return (dynamic)new DynamicHubCollection(this); } }

        ~PomeliumClient()
        {
            try
            {
                _tcpClient.Send(new Packet
                {
                    SessionId = SessionId,
                    Type = PacketType.Disconnect
                });
            }
            catch { }
        }
    }
}
