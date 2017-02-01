using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Dynamic;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;

namespace Pomelo.Net.Pomelium.Server
{
    public class PomeliumClientOnServerSide : DynamicObject
    {
        public Guid SessionId { get; set; }

        private TcpClient _tcpClient;
        private PomeliumServer _pomeliumServer;
        private static FieldInfo _remoteTaskSemaphoreFieldInfo = typeof(PomeliumServer).GetTypeInfo().DeclaredFields.Single(x => x.Name == "_remoteTaskSemaphore");

        public PomeliumClientOnServerSide(TcpClient tcpClient, PomeliumServer server)
        {
            _tcpClient = tcpClient;
            _pomeliumServer = server;
        }

        public TcpClient TcpClient => _tcpClient;

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var packet = new Packet
            {
                Method = binder.Name,
                Arguments = args,
                RequestId = Guid.NewGuid()
            };
            var dic = (Dictionary<Guid, TaskCompletionSource<object>>)_remoteTaskSemaphoreFieldInfo.GetValue(_pomeliumServer);
            TcpClient.SendAsync(packet);
            dic.Add(packet.RequestId, new TaskCompletionSource<object>());
            result = dic[packet.RequestId].Task;
            return true;
        }

    }
}
