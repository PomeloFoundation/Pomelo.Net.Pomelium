using Pomelo.Net.Pomelium.Server;
using Pomelo.Net.Pomelium.Server.Session;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class PomeliumServerServiceCollectionExtensions
    {
        public static IServiceCollection AddPomeliumServer(this IServiceCollection self)
        {
            return self.AddSingleton<PomeliumServer>()
                .AddSingleton<ISession, MemorySession>()
                .AddSingleton<IPomeliumHubLocator, DefaultPomeliumHubLocator>();
        }
    }
}
