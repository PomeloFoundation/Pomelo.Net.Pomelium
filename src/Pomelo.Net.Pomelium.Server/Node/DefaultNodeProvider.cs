using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Distributed;
using Pomelo.Net.Pomelium.Server.Extensions;
using Pomelo.Net.Pomelium.Server.ServerIdentifier;
using Pomelo.Net.Pomelium.Server.Semaphore;

namespace Pomelo.Net.Pomelium.Server.Node
{
    public class DefaultNodeProvider : INodeProvider
    {
        private Timer _timer;
        private HashSet<NodeInfo> _nodeInfo;
        private PomeliumOptions _pomeliumOptions;
        private IDistributedCache _distributedCache;
        private IServerIdentifier _serverIdentifier;
        private ISemaphoreProvider _semaphoreProvider;
        private ConcurrentDictionary<Guid, Node>_nodes = new ConcurrentDictionary<Guid, Node>();
        private volatile bool _lock = false;
        private volatile bool _needCollect = false;

        public DefaultNodeProvider(PomeliumOptions pomeliumOptions, IDistributedCache distributedCache, IServerIdentifier serverIdentifier, ISemaphoreProvider semaphoreProvider)
        {
            _pomeliumOptions = pomeliumOptions;
            _distributedCache = distributedCache;
            _serverIdentifier = serverIdentifier;
            _semaphoreProvider = semaphoreProvider;
            _timer = new Timer(TimeIntervalCallBack, null, 0, 30 * 1000);
        }

        protected virtual async void TimeIntervalCallBack(object state)
        {
            if (_lock)
                return;
            _lock = true;
            _needCollect = false;
            try
            {
                var json = await _distributedCache.GetStringAsync(_pomeliumOptions.NodeCachingPrefix) ?? "[]";
                _nodeInfo = new HashSet<NodeInfo>(JsonConvert.DeserializeObject<IEnumerable<NodeInfo>>(json));
                if (!_nodeInfo.Any(x => x.ServerId == _serverIdentifier.GetIdentifier()))
                {
                    _nodeInfo.Add(new NodeInfo
                    {
                        AddressList = (await DnsResolutionAsync(_pomeliumOptions.Address)).Select(x => x.ToString()),
                        Port = _pomeliumOptions.Port,
                        ServerId = _serverIdentifier.GetIdentifier()
                    });
                    await _distributedCache.SetStringAsync(_pomeliumOptions.NodeCachingPrefix, JsonConvert.SerializeObject(_nodeInfo));
                }
                Parallel.ForEach(_nodes.Where(x => !_nodeInfo.Any(y => y.ServerId == x.Key)), x =>
                {
                    Node tmp;
                    _nodes.TryRemove(x.Key, out tmp);
                    _needCollect = true;
                });
                Parallel.ForEach(_nodeInfo.Where(x => !_nodes.Any(y => y.Key == x.ServerId)), x =>
                {
                    if (x.ServerId != _serverIdentifier.GetIdentifier())
                    {
                        try
                        {
                            _nodes.AddOrUpdate(x.ServerId, new Node(x, _serverIdentifier, _semaphoreProvider), (y, z) => z);
                        }
                        catch
                        {
                            _nodeInfo.Remove(x);
                            lock (this)
                            {
                                _distributedCache.SetStringAsync(_pomeliumOptions.NodeCachingPrefix, JsonConvert.SerializeObject(_nodeInfo));
                            }
                        }
                    }
                });
                if (_needCollect)
                {
                    GC.Collect();
                }
            }
            finally
            {
                _lock = false;
            }
        }

        protected virtual async Task<IEnumerable<IPAddress>> DnsResolutionAsync(string address)
        {
            var result = await Dns.GetHostEntryAsync(address);
            return result.AddressList;
        }

        public virtual Node FindNodeById(Guid id) => _nodes.ContainsKey(id) ? _nodes[id] : null;

        public virtual IEnumerable<Node> Nodes
        {
            get
            {
                return _nodes.Select(x => x.Value);
            }
        }
    }
}
