using System;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public interface ISession
    {
        Task<object> GetAsync(Guid SessionId, string Key);
        Task SetAsync(Guid SessionId, string Key, object Value);
    }
}
