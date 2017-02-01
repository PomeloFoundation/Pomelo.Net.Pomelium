using System;
using System.Collections.Generic;
using System.Net;

namespace Pomelo.Net.Pomelium.Server.Node
{
    public class NodeInfo
    {
        public IEnumerable<string> AddressList { get; set; }

        public int Port { get; set; }

        public Guid ServerId { get; set; }
    }
}
