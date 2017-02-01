using System;
using Pomelo.Net.Pomelium.Server;
using Pomelo.Net.Pomelium.Server.Session;
using Pomelo.Net.Pomelium.Server.Semaphore;
using Pomelo.Net.Pomelium.Server.HubActivitor;
using Pomelo.Net.Pomelium.Server.ServerIdentifier;
using Pomelo.Net.Pomelium.Server.Node;
using Pomelo.Net.Pomelium.Server.Client;
using Pomelo.Net.Pomelium.Server.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PomeliumServerServiceCollectionExtensions
    {
        public static IServiceCollection AddPomeliumServer<T>(this IServiceCollection self, Action<PomeliumOptions> setup = null)
            where T : PomeliumServer
        {
            var option = new PomeliumOptions();
            setup?.Invoke(option);
            return self.AddSingleton<T>()
                .AddSingleton<ISession, DistributedSession>()
                .AddSingleton<IPomeliumHubLocator, DefaultPomeliumHubLocator>()
                .AddSingleton<ISemaphoreProvider, DefaultSemaphoreProvider>()
                .AddSingleton<IHubActivitor, DefaultHubActivitor>()
                .AddSingleton<IServerIdentifier, DefaultServerIdentifier>()
                .AddSingleton<INodeProvider, DefaultNodeProvider>()
                .AddSingleton<IClientCollection, DefaultClientCollection>()
                .AddSingleton(option);
        }

        public static IServiceCollection AddPomeliumServer(this IServiceCollection self, Action<PomeliumOptions> setup = null)
        {
            return AddPomeliumServer<PomeliumServer>(self, setup);
        }
    }
}
