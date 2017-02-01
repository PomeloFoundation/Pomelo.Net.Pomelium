using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;
using System.Net.Sockets;
using Pomelo.Net.Pomelium.Server.Semaphore;

namespace Pomelo.Net.Pomelium.Server.Client
{
    public class LocalClient : DynamicObject, IClient
    {
        private PomeliumServer _pomeliumServer;
        private TcpClient _tcpClient;
        private ISemaphoreProvider _semaphoreProvider;

        public LocalClient(TcpClient tcpClient, ISemaphoreProvider semaphoreProvider)
        {
            _tcpClient = tcpClient;
            _semaphoreProvider = semaphoreProvider;
        }

        public bool IsLocal => true;

        public Guid SessionId { get; set; }

        public virtual async Task<object> InvokeAsync(string method, object[] args)
        {
            var packet = new Packet
            {
                Method = method,
                Arguments = args,
                RequestId = Guid.NewGuid()
            };
            await _tcpClient.SendAsync(packet);
            var requestId =_semaphoreProvider.Create();
            return await _semaphoreProvider.GetTaskById(requestId);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = InvokeAsync(binder.Name, args);
            return true;
        }

        public Task SendAsync(Packet packet)
        {
            return _tcpClient.SendAsync(packet);
        }
    }
}
