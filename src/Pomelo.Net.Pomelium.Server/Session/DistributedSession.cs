using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Pomelo.Net.Pomelium.Server.Async;
using Pomelo.Net.Pomelium.Server.Extensions;
using Newtonsoft.Json;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public class DistributedSession : SessionBase
    {
        private readonly string _prefix;
        private IDistributedCache _distributedCache;
        private PomeliumOptions _pomeliumOptions;
        private AsyncLockers _asyncLockers;

        public DistributedSession(
            IDistributedCache distributedCache, 
            PomeliumOptions pomeliumOptions,
            AsyncLockers asyncLockers)
        {
            _distributedCache = distributedCache;
            _pomeliumOptions = pomeliumOptions;
            _prefix = _pomeliumOptions.SessionCachingPrefix;
            _asyncLockers = asyncLockers;
        }

        public virtual string BuildPrefix(Guid SessionId, string Key)
        {
            return _prefix + SessionId + "_" + Key;
        }

        public override async Task<object> GetAsync(Guid SessionId, string Key)
        {
            return JsonConvert.DeserializeObject(await _distributedCache.GetStringAsync(BuildPrefix(SessionId, Key)).ConfigureAwait(false));
        }

        public override async Task SetAsync(Guid SessionId, string Key, object Value)
        {
            await _asyncLockers.SessionOperationLocker.WaitAsync();
            try
            {
                await _distributedCache.SetStringAsync(BuildPrefix(SessionId, Key), JsonConvert.SerializeObject(Value));
            }
            finally
            {
                _asyncLockers.SessionOperationLocker.Release();
            }
            await _asyncLockers.ClientOwnedSessionKeysOperationLocker.WaitAsync();
            try
            {
                var keys = JsonConvert.DeserializeObject<List<string>>(await _distributedCache.GetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId) ?? "[]");
                if (!keys.Contains(Key))
                {
                    keys.Add(Key);
                    await _distributedCache.SetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId, JsonConvert.SerializeObject(keys));
                }
            }
            finally
            {
                _asyncLockers.ClientOwnedSessionKeysOperationLocker.Release();
            }
        }

        public override async Task RemoveAsync(Guid SessionId, string Key)
        {
            await _asyncLockers.SessionOperationLocker.WaitAsync();
            try
            {
                await _distributedCache.RemoveAsync(BuildPrefix(SessionId, Key));
            }
            finally
            {
                _asyncLockers.SessionOperationLocker.Release();
            }
            await _asyncLockers.ClientOwnedSessionKeysOperationLocker.WaitAsync();
            try
            {
                var keys = JsonConvert.DeserializeObject<List<string>>(await _distributedCache.GetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId) ?? "[]");
                if (keys.Contains(Key))
                {
                    keys.Remove(Key);
                    await _distributedCache.SetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId, JsonConvert.SerializeObject(keys));
                }
            }
            finally
            {
                _asyncLockers.ClientOwnedSessionKeysOperationLocker.Release();
            }
        }

        public override async Task<IEnumerable<string>> GetKeysAsync(Guid SessionId)
        {
            var keys = JsonConvert.DeserializeObject<List<string>>(await _distributedCache.GetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId) ?? "[]");
            return keys;
        }

        public override async Task<bool> ExistsAsync(Guid SessionId)
        {
            return string.IsNullOrWhiteSpace(await _distributedCache.GetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId));
        }

        public override async Task InitAsync(Guid SessionId)
        {
            await _distributedCache.SetStringAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + SessionId, "[]");
        }
    }
}
