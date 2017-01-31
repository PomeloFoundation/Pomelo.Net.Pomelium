using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Session
{
    public class MemorySession : SessionBase
    {
        private Dictionary<Guid, Dictionary<string, object>> _dic = new Dictionary<Guid, Dictionary<string, object>>();

        public override async Task<object> GetAsync(Guid SessionId, string Key)
        {
            EnsureSessionId(SessionId);
            if (_dic[SessionId].ContainsKey(Key))
                return _dic[SessionId][Key];
            else
                return null;
        }

        public override async Task SetAsync(Guid SessionId, string Key, object Value)
        {
            EnsureSessionId(SessionId);
            if (_dic[SessionId].ContainsKey(Key))
                _dic[SessionId].Add(Key, Value);
            else
                _dic[SessionId][Key] = Value;
        }

        protected void EnsureSessionId(Guid Id)
        {
            if (!_dic.ContainsKey(Id))
            {
                _dic.Add(Id, new Dictionary<string, object>());
            }
        }
    }
}
