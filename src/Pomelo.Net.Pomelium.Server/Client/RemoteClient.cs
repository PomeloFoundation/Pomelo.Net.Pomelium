using System;
using System.Threading.Tasks;
using System.Dynamic;

namespace Pomelo.Net.Pomelium.Server.Client
{
    public class RemoteClient : DynamicObject, IClient
    {
        public bool IsLocal => false;

        public Guid SessionId { get; set; }

        public Node.Node Node { get; set; }

        public Task<dynamic> InvokeAsync(string method, object[] args)
        {
            return Node.InvokeAsync(SessionId, method, args);
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = InvokeAsync(binder.Name, args);
            return true;
        }

        public Task SendAsync(Packet packet)
        {
            return Node.InvokeAsync(SessionId, packet.Method, packet.Arguments);
        }
    }
}
