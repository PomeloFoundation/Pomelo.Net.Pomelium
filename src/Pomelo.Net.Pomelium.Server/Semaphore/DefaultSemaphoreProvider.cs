using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Semaphore
{
    public class DefaultSemaphoreProvider : ISemaphoreProvider
    {
        private Dictionary<Guid, TaskCompletionSource<object>> _dic = new Dictionary<Guid, TaskCompletionSource<object>>();

        public Guid Create()
        {
            var id = Guid.NewGuid();
            _dic.Add(id, new TaskCompletionSource<object>());
            return id;
        }

        public Task<object> GetTaskById(Guid id)
        {
            return _dic[id].Task;
        }

        public void SetException(Guid id, Exception value)
        {
            _dic[id].SetException(value);
            _dic.Remove(id);
        }

        public void SetResult(Guid id, object value)
        {
            _dic[id].SetResult(value);
            _dic.Remove(id);
        }
    }
}
