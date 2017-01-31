using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public class DistributedSession : SessionBase
    {
        private static readonly string _prefix = "__commlite__";
        private IDistributedCache _distributedCache;

        public DistributedSession(IDistributedCache distributedCache)
        {
            _distributedCache = distributedCache;
        }

        protected virtual string BuildPrefix(Guid SessionId, string Key)
        {
            return _prefix + SessionId + Key;
        }

        public override async Task<object> GetAsync(Guid SessionId, string Key)
        {
            return JsonConvert.DeserializeObject(await _distributedCache.GetStringAsync(BuildPrefix(SessionId, Key)).ConfigureAwait(false));
        }

        public override async Task SetAsync(Guid SessionId, string Key, object Value)
        {
            await _distributedCache.SetStringAsync(BuildPrefix(SessionId, Key), JsonConvert.SerializeObject(Value));
        }
    }
}
