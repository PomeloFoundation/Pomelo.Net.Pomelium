using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.ServerIdentifier
{
    public interface IServerIdentifier
    {
        Guid GetIdentifier();
    }
}
