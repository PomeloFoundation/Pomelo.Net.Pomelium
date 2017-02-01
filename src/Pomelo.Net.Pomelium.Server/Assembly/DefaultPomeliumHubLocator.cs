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
        private HashSet<Type> _hubs;

#if NET451
        public IEnumerable<Type> GetHubs()
        {
            if (_hubs == null)
                _hubs = new HashSet<Type>(AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsSubclassOf(typeof(PomeliumHub))));
            return _hubs;
        }
#else
        public virtual IEnumerable<Type> GetHubs()
        {
            if (_hubs == null)
            {
                var assembly = GetEntryAssembly();
                _hubs = new HashSet<Type>(assembly.GetTypes().Where(x => x.GetTypeInfo().IsSubclassOf(typeof(PomeliumHub))));
            }
            return _hubs;
        }

        private static Assembly GetEntryAssembly()
        {
            var getEntryAssemblyMethod =
                typeof(Assembly).GetMethod("GetEntryAssembly", BindingFlags.Static | BindingFlags.NonPublic) ??
                typeof(Assembly).GetMethod("GetEntryAssembly", BindingFlags.Static | BindingFlags.Public);
            return getEntryAssemblyMethod.Invoke(obj: null, parameters: Array.Empty<object>()) as Assembly;
        }
#endif

        public virtual Type FindHubByClassName(string className)
        {
            return GetHubs().SingleOrDefault(x => x.Name == className || x.Name == className + "Hub");
        }
    }
}
