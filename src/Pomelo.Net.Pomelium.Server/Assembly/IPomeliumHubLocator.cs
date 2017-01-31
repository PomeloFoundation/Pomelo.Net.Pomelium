using System;
using System.Collections.Generic;

namespace Pomelo.Net.Pomelium.Server
{
    public interface IPomeliumHubLocator
    {
        IEnumerable<Type> GetHubs();
    }
}
