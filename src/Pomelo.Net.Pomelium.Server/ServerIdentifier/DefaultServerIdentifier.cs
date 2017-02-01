using System;

namespace Pomelo.Net.Pomelium.Server.ServerIdentifier
{
    public class DefaultServerIdentifier : IServerIdentifier
    {
        private Guid _identifier;

        public DefaultServerIdentifier()
        {
            _identifier = Guid.NewGuid();
        }

        public Guid GetIdentifier()
        {
            return _identifier;
        }
    }
}
