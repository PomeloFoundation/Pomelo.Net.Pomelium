using System;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public class SessionCollection
    {
        private ISession _session;
        private Guid _sessionId;

        public SessionCollection(ISession session, Guid sessionId)
        {
            _session = session;
            _sessionId = sessionId;
        }

        public Task<object> GetAsync(string key)
        {
            return _session.GetAsync(_sessionId, key);
        }

        public Task SetAsync(string key, object value)
        {
            return _session.SetAsync(_sessionId, key, value);
        }

        public object this[string key]
        {
            get
            {
                var task = _session.GetAsync(_sessionId, key);
                task.Wait();
                return task.Result;
            }
            set
            {
                var task = _session.SetAsync(_sessionId, key, value);
                task.Wait();
            }
        }
    }
}
