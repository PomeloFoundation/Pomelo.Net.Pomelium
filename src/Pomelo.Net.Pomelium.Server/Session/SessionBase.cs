using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public abstract class SessionBase : ISession
    {
        public abstract Task<object> GetAsync(Guid SessionId, string Key);
        public abstract Task SetAsync(Guid SessionId, string Key, object Value);
    }
}
