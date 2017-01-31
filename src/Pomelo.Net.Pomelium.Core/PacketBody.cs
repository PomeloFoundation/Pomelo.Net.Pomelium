using System;

namespace Pomelo.Net.Pomelium
{
    public class PacketBody
    {
        public PacketType Type { get; set; }
        public Guid RequestId { get; set; }
        public Guid? SessionId { get; set; }
        public string Hub { get; set; }
        public string Method { get; set; }
        public object[] Arguments { get; set; }
        public object ReturnValue { get; set; }
        public int Code { get; set; } = 200;
    }
}
