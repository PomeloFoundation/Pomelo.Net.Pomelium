using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public interface ISession
    {
        Task<object> GetAsync(Guid SessionId, string Key);
        Task SetAsync(Guid SessionId, string Key, object Value);
        Task RemoveAsync(Guid SessionId, string Key);
        Task<IEnumerable<string>> GetKeysAsync(Guid SessionId);
        Task<bool> ExistsAsync(Guid SessionId);
        Task InitAsync(Guid SessionId);
    }
}
