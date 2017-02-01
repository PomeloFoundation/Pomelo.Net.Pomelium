using System;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Client
{
    public interface IClient
    {
        bool IsLocal { get; }

        Guid SessionId { get; }

        Task<object> InvokeAsync(string method, object[] args);

        Task SendAsync(Packet packet);
    }
}
