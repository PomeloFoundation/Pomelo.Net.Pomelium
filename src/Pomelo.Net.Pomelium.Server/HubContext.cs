using System;
using Pomelo.Net.Pomelium.Server.Session;

namespace Pomelo.Net.Pomelium.Server
{
    public class HubContext
    {
        public Packet Request { get; set; }
        public dynamic Client { get; set; }
        public Guid SessionId { get; set; }
        public SessionCollection Session { get; set; }
        public IServiceProvider Resolver { get; set; }
    }
}
