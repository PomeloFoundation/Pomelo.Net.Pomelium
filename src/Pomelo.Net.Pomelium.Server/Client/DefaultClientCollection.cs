using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.Extensions;
using Pomelo.Net.Pomelium.Server.Node;
using Pomelo.Net.Pomelium.Server.ServerIdentifier;
using Pomelo.Net.Pomelium.Server.Async;

namespace Pomelo.Net.Pomelium.Server.Client
{
    public class DefaultClientCollection : IClientCollection
    {
        private PomeliumOptions _pomeliumOptions;
        private IDistributedCache _distributedCache;
        private INodeProvider _nodeProvider;
        private IServerIdentifier _serverIdentifier;
        private Dictionary<Guid, IClient> _dic;
        private AsyncSemaphore _lockerAddGroup = new AsyncSemaphore();
        private AsyncSemaphore _lockerRemoveGroup = new AsyncSemaphore();
        private AsyncSemaphore _lockerNodeClients = new AsyncSemaphore();

        public DefaultClientCollection(PomeliumOptions pomeliumOptions, IDistributedCache distributedCache, INodeProvider nodeProvider, IServerIdentifier serverIdentifier)
        {
            _pomeliumOptions = pomeliumOptions;
            _distributedCache = distributedCache;
            _nodeProvider = nodeProvider;
            _serverIdentifier = serverIdentifier;
            _dic = new Dictionary<Guid, IClient>(); ;
        }

        public async Task AddClientIntoGroupAsync(IClient client, string group)
        {
            await _lockerAddGroup.WaitAsync();
            var sessionIds = JsonConvert.DeserializeObject<List<Guid>>(await _distributedCache.GetStringAsync(_pomeliumOptions.GroupsCachingPrefix + group.ToUpper()) ?? "[]");
            if (sessionIds.Contains(client.SessionId))
                return;
            sessionIds.Add(client.SessionId);
            await _distributedCache.SetStringAsync(_pomeliumOptions.GroupsCachingPrefix + group.ToUpper(), JsonConvert.SerializeObject(sessionIds.Distinct()));
            _lockerAddGroup.Release();
        }

        public async Task<IClient> GetClientAsync(Guid sessionId)
        {
            // Local
            if (_dic.Select(x => x.Value).Any(x => x.IsLocal == true && x.SessionId == sessionId))
            {
                return _dic[sessionId];
            }

            // Remote
            var serverId = await FindServerBySessionIdAsync(sessionId);
            var client = new RemoteClient() { Node = _nodeProvider.FindNodeById(serverId), SessionId = sessionId };
            return client;
        }

        protected virtual async Task<Guid> FindServerBySessionIdAsync(Guid sessionId)
        {
            var tasks = new List<Task>();
            foreach (var x in _nodeProvider.Nodes.Select(x => x.NodeInfo))
            {
                tasks.Add(Task.Run(async ()=> 
                {
                    var clients = JsonConvert.DeserializeObject<List<ClientInfo>>(await _distributedCache.GetStringAsync(_pomeliumOptions.ClientsCachingPrefix + x.ServerId) ?? "[]");
                    if (clients.Any(y => y.SessionId == sessionId))
                    {
                        return clients.Single(y => y.SessionId == sessionId).ServerId;
                    }
                    else
                    {
                        throw new KeyNotFoundException();
                    }
                }));
            }
            var serverId = await ((Task<Guid>)await Task.WhenAny(tasks.ToArray()));
            return serverId;
        }

        public async Task StoreLocalClientAsync(LocalClient client)
        {
            await _lockerNodeClients.WaitAsync();

            if (_dic.ContainsKey(client.SessionId))
                _dic[client.SessionId] = client;
            else
                _dic.Add(client.SessionId, client);

            var clientInfo = _dic.Select(x => new ClientInfo
            {
                HashCode = x.Value.GetHashCode(),
                ServerId = _serverIdentifier.GetIdentifier(),
                SessionId = x.Key
            });
            await _distributedCache.SetStringAsync(_pomeliumOptions.ClientsCachingPrefix + _serverIdentifier.GetIdentifier(), JsonConvert.SerializeObject(clientInfo));
            _lockerNodeClients.Release();
        }

        public async Task RemoveClientFromGroupAsync(IClient client, string group)
        {
            await _lockerRemoveGroup.WaitAsync();
            var sessionIds = JsonConvert.DeserializeObject<List<Guid>>(await _distributedCache.GetStringAsync(_pomeliumOptions.GroupsCachingPrefix + group.ToUpper()) ?? "[]");
            if (!sessionIds.Contains(client.SessionId))
                return;
            sessionIds.Remove(client.SessionId);
            await _distributedCache.SetStringAsync(_pomeliumOptions.GroupsCachingPrefix + group.ToUpper(), JsonConvert.SerializeObject(sessionIds.Distinct()));
            _lockerAddGroup.Release();
        }

        public async Task<IEnumerable<IClient>> GetGroupClientsAsync(string group)
        {
            var ids = JsonConvert.DeserializeObject<List<Guid>>(await _distributedCache.GetStringAsync(_pomeliumOptions.GroupsCachingPrefix + group.ToUpper()) ?? "[]");
            var ret = new ConcurrentBag<IClient>();
            var tasks = new List<Task>();
            foreach (var x in ids)
            {
                tasks.Add(Task.Run(async () =>
                {
                    ret.Add(await GetClientAsync(x));
                }));
            }
            Task.WaitAll(tasks.ToArray());
            return ret;
        }

        public bool LocalClientExist(Guid sessionId)
        {
            return _dic.ContainsKey(sessionId);
        }

        public async Task RemoveLocalClientAsync(LocalClient client)
        {
            await _lockerNodeClients.WaitAsync();

            if (_dic.ContainsKey(client.SessionId))
                _dic.Remove(client.SessionId);
            else
                return;

            var clientInfo = _dic.Select(x => new ClientInfo
            {
                HashCode = x.Value.GetHashCode(),
                ServerId = _serverIdentifier.GetIdentifier(),
                SessionId = x.Key
            });
            await _distributedCache.SetStringAsync(_pomeliumOptions.ClientsCachingPrefix + _serverIdentifier.GetIdentifier(), JsonConvert.SerializeObject(clientInfo));
            _lockerNodeClients.Release();
        }
    }
}
