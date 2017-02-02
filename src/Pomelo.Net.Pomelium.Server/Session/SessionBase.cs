using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public abstract class SessionBase : ISession
    {
        public abstract Task<object> GetAsync(Guid SessionId, string Key);
        public abstract Task SetAsync(Guid SessionId, string Key, object Value);
        public abstract Task RemoveAsync(Guid SessionId, string Key);
        public abstract Task<IEnumerable<string>> GetKeysAsync(Guid SessionId);
        public abstract Task<bool> ExistsAsync(Guid SessionId);
        public abstract Task InitAsync(Guid SessionId);
    }
}
