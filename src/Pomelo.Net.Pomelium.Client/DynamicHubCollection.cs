using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Dynamic;

namespace Pomelo.Net.Pomelium.Client
{
    public class DynamicHubCollection
    {
        public dynamic this[string hub]
        {
            get
            {
                return (dynamic)new DynamicHub(hub, _client);
            }
        }
        
        private PomeliumClient _client;

        public DynamicHubCollection(PomeliumClient client)
        {
            _client = client;
        }
    }
}