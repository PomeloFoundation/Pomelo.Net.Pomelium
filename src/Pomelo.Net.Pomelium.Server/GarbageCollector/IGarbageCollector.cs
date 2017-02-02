using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.GarbageCollector
{
    public interface IGarbageCollector
    {
        Task MarkGarbageAsync(Guid sessionId);
        Task UnmarkGarbageAsync(Guid sessionId);
        Task<bool> IsInBufferAsync(Guid sessionId);
        Task<IEnumerable<GarbageInfo>> GetGarbagesAsync();
    }
}
