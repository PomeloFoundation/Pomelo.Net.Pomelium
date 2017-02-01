using System;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Semaphore
{
    public interface ISemaphoreProvider
    {
        Guid Create();
        void SetResult(Guid id, object value);
        void SetException(Guid id, Exception value);
        Task<object> GetTaskById(Guid id);
    }
}
