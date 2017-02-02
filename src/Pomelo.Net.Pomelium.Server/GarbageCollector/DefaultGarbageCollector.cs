using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.Async;
using Pomelo.Net.Pomelium.Server.Client;
using Pomelo.Net.Pomelium.Server.Session;
using Pomelo.Net.Pomelium.Server.Extensions;
using Pomelo.Net.Pomelium.Server.ServerIdentifier;

namespace Pomelo.Net.Pomelium.Server.GarbageCollector
{
    public class DefaultGarbageCollector : IGarbageCollector
    {
        private PomeliumOptions _pomeliumOptions;
        private IClientCollection _clientCollection;
        private IDistributedCache _distributedCache;
        private ISession _session;
        private IServerIdentifier _serverIdentifier;
        private Timer _collectTimer;
        private AsyncLockers _asyncLockers;
        private volatile bool _collectLock = false;

        public DefaultGarbageCollector(
            PomeliumOptions pomeliumOptions, 
            IClientCollection clientCollection,
            IDistributedCache distributedCache,
            ISession session,
            IServerIdentifier serverIdentifier,
            AsyncLockers asyncLockers)
        {
            _pomeliumOptions = pomeliumOptions;
            _clientCollection = clientCollection;
            _distributedCache = distributedCache;
            _session = session;
            _serverIdentifier = serverIdentifier;
            _collectTimer = new Timer(CollectCallback, null, 0, 1000 * 60);
            _asyncLockers = asyncLockers;
        }

        protected virtual async void CollectCallback(object state)
        {
            if (_collectLock)
                return;
            _collectLock = true;
            try
            {
                var garbages = (await GetGarbagesAsync()).Where(x => x.CollectServer == _serverIdentifier.GetIdentifier() && DateTime.Now >= x.ExpireTime);
                foreach(var x in garbages)
                {
                    try
                    {
                        // Clean group member
                        foreach (var g in await _clientCollection.GetJoinedGroupAsync(x.SessionId))
                        {
                            await _clientCollection.RemoveClientFromGroupAsync(x.SessionId, g);
                        }
                        await _distributedCache.RemoveAsync(_pomeliumOptions.ClientJoinedGroupsCachingPrefix + x.SessionId);

                        // Clean session
                        foreach (var s in await _session.GetKeysAsync(x.SessionId))
                        {
                            await _session.RemoveAsync(x.SessionId, s);
                        }
                        await _distributedCache.RemoveAsync(_pomeliumOptions.ClientOwnedSessionKeysCachingPrefix + x.SessionId);
                    }
                    finally
                    {
                    }
                }
            }
            finally
            {
                _collectLock = false;
            }
        }

        public async Task MarkGarbageAsync(Guid sessionId)
        {
            var garbages = (await GetGarbagesAsync()).ToList();
            if (!garbages.Any(x => x.SessionId == sessionId))
            {
                garbages.Add(new GarbageInfo
                {
                    CollectServer = _serverIdentifier.GetIdentifier(),
                    ExpireTime = DateTime.Now.Add(_pomeliumOptions.GarbageCollectBufferTimeSpan),
                    SessionId = sessionId
                });
                await _distributedCache.SetStringAsync(_pomeliumOptions.GCCachingPrefix, JsonConvert.SerializeObject(garbages));
            }
        }

        public async Task UnmarkGarbageAsync(Guid sessionId)
        {
            var garbages = (await GetGarbagesAsync()).ToList();
            if (garbages.Any(x => x.SessionId == sessionId))
            {
                garbages.Remove(garbages.Single(x => x.SessionId == sessionId));
                await _distributedCache.SetStringAsync(_pomeliumOptions.GCCachingPrefix, JsonConvert.SerializeObject(garbages));
            }
        }

        public async Task<bool> IsInBufferAsync(Guid sessionId)
        {
            return (await GetGarbagesAsync()).Any(x => x.SessionId == sessionId);
        }

        public async Task<IEnumerable<GarbageInfo>> GetGarbagesAsync()
        {
            return JsonConvert.DeserializeObject<IEnumerable<GarbageInfo>>(await _distributedCache.GetStringAsync(_pomeliumOptions.GCCachingPrefix) ?? "[]");
        }
    }
}
