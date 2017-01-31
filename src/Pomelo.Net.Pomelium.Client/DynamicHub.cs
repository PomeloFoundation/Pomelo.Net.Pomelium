using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;
using System.Reflection;
using Pomelo.Net.Pomelium;

namespace Pomelo.Net.Pomelium.Client
{
    public class DynamicHub : DynamicObject
    {
        private string _hubName;
        private PomeliumClient _client;
        private static TypeInfo _commLiteClientTypeInfo = typeof(PomeliumClient).GetTypeInfo();
        private static FieldInfo _remoteTaskSemaphoreFieldInfo = _commLiteClientTypeInfo.DeclaredFields.Single(x => x.Name == "_remoteTaskSemaphore");

        public DynamicHub(string hubName, PomeliumClient client)
        {
            _hubName = hubName;
            _client = client;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            var packet = new PacketBody
            {
                Hub = _hubName,
                Method = binder.Name,
                Arguments = args,
                RequestId = Guid.NewGuid(),
                SessionId = _client.SessionId
            };
            var dic = (Dictionary<Guid, TaskCompletionSource<object>>)_remoteTaskSemaphoreFieldInfo.GetValue(_client);
            _client.TcpClient.SendAsync(packet);
            dic.Add(packet.RequestId, new TaskCompletionSource<object>());
            result = dic[packet.RequestId].Task;
            return true;
        }
    }
}