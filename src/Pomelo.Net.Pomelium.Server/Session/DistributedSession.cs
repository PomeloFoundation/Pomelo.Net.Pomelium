using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Pomelo.Net.Pomelium.Server.Extensions;
using Newtonsoft.Json;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public class DistributedSession : SessionBase
    {
        private readonly string _prefix;
        private IDistributedCache _distributedCache;
        private PomeliumOptions _pomeliumOptions;

        public DistributedSession(IDistributedCache distributedCache, PomeliumOptions pomeliumOptions)
        {
            _distributedCache = distributedCache;
            _pomeliumOptions = pomeliumOptions;
            _prefix = _pomeliumOptions.SessionCachingPrefix;
        }

        protected virtual string BuildPrefix(Guid SessionId, string Key)
        {
            return _prefix + SessionId + "_" + Key;
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
