using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if !NET451
using Microsoft.Extensions.PlatformAbstractions;
#endif

namespace Pomelo.Net.Pomelium.Server
{
    public class DefaultPomeliumHubLocator : IPomeliumHubLocator
    {
#if NET451
        public IEnumerable<Type> GetHubs()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsSubclassOf(typeof(PomeliumHub)));
        }
#else
        public IEnumerable<Type> GetHubs()
        {
            var assembly = GetEntryAssembly();
            return assembly.GetTypes().Where(x => x.GetTypeInfo().IsSubclassOf(typeof(PomeliumHub)));
        }

        private static Assembly GetEntryAssembly()
        {
            var getEntryAssemblyMethod =
                typeof(Assembly).GetMethod("GetEntryAssembly", BindingFlags.Static | BindingFlags.NonPublic) ??
                typeof(Assembly).GetMethod("GetEntryAssembly", BindingFlags.Static | BindingFlags.Public);
            return getEntryAssemblyMethod.Invoke(obj: null, parameters: Array.Empty<object>()) as Assembly;
        }
#endif
    }
}
