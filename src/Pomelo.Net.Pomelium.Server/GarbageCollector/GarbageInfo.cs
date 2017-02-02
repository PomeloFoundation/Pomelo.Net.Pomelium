using System;

namespace Pomelo.Net.Pomelium.Server.GarbageCollector
{
    public class GarbageInfo
    {
        public Guid SessionId { get; set; }

        public DateTime ExpireTime { get; set; }

        public Guid CollectServer { get; set; }
    }
}
