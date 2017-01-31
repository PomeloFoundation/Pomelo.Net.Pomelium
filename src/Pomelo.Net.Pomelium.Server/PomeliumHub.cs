using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server
{
    public class PomeliumHub
    {
        public PomeliumContext Context { get; set; }

        protected virtual void OnInvoking()
        {
        }

        protected virtual void OnInvoked()
        {
        }
    }
}
