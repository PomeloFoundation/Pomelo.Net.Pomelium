using System;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Node
{
    public interface INode
    {
        Task<object> InvokeAsync(Guid sessionId, string method, object[] args);
    }
}
