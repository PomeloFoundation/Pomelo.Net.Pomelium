using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Client
{
    public interface IClientCollection
    {
        Task StoreLocalClientAsync(LocalClient client);
        Task RemoveLocalClientAsync(LocalClient client);
        Task<IClient> GetClientAsync(Guid sessionId);
        Task AddClientIntoGroupAsync(IClient client, string group);
        Task RemoveClientFromGroupAsync(IClient client, string group);
        Task AddClientIntoGroupAsync(Guid sessionId, string group);
        Task RemoveClientFromGroupAsync(Guid sessionId, string group);
        Task<IEnumerable<string>> GetJoinedGroupAsync(Guid sessionId);
        Task<IEnumerable<IClient>> GetGroupClientsAsync(string group);
        IEnumerable<LocalClient> GetLocalClients();
        bool LocalClientExist(Guid sessionId);
    }
}
