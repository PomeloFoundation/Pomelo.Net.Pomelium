using System;

namespace Pomelo.Net.Pomelium.Server.Client
{
    public class ClientInfo
    {
        public Guid SessionId { get; set; }

        public int HashCode { get; set; }

        public Guid ServerId { get; set; }
    }
}
