using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Extensions
{
    public class PomeliumOptions
    {
        public string Address { get; set; } = "";
        public int Port { get; set; } = 6000;
        public string NodeCachingPrefix { get; set; } = "POMELIUM_NODES";
        public string SessionCachingPrefix { get; set; } = "POMELIUM_SESSION_";
        public string ClientsCachingPrefix { get; set; } = "POMELIUM_CLIENTS_";
        public string GroupsCachingPrefix { get; set; } = "POMELIUM_GROUP_";
        public string ClientJoinedGroupsCachingPrefix { get; set; } = "POMELIUM_JOINED_GROUP_";
        public string ClientOwnedSessionKeysCachingPrefix { get; set; } = "POMELIUM_CLIENT_SESSION_KEYS_";
        public string GCCachingPrefix { get; set; } = "POMELIUM_GARBAGE_COLLECT";
        public TimeSpan GarbageCollectBufferTimeSpan = new TimeSpan(0, 5, 0);
    }
}
