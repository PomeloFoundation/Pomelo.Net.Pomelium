using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reflection;
using Newtonsoft.Json;
using Pomelo.Net.Pomelium.Server.Client;
using Pomelo.Net.Pomelium.Server.Session;

namespace Pomelo.Net.Pomelium.Server.HubActivitor
{
    public class DefaultHubActivitor : IHubActivitor
    {
        private IPomeliumHubLocator _pomeliumHubLocator;
        private IServiceProvider _serviceProvider;
        private ISession _session;

        public DefaultHubActivitor(IPomeliumHubLocator pomeliumHubLocator, IServiceProvider serviceProvider, ISession session)
        {
            _pomeliumHubLocator = pomeliumHubLocator;
            _serviceProvider = serviceProvider;
            _session = session;
        }

        public virtual PomeliumHub CreateInstance(string hubName, IClient client, Packet request)
        {
            var hubType = _pomeliumHubLocator.FindHubByClassName(hubName);
            var hub = (PomeliumHub)Activator.CreateInstance(hubType);
            hub.Context = new HubContext {
                Client = client,
                Request = request,
                SessionId = request.SessionId.Value,
                Resolver = _serviceProvider,
                Session = new SessionCollection(_session, request.SessionId.Value)
            };
            return hub;
        }

        public virtual MethodInfo GetMethod(PomeliumHub instance, string methodName)
        {
            return instance.GetType().GetTypeInfo().GetMethod(methodName);
        }

        public virtual HubMethodType GetReturnValueType(MethodInfo methodInfo)
        {
            try
            {
                if (methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    return HubMethodType.TaskWithReturnValue;
                }
            }
            catch
            {
            }
            if (methodInfo.ReturnType == typeof(Task))
            {
                return HubMethodType.Task;
            }
            else if (methodInfo.ReturnType == typeof(void))
            {
                return HubMethodType.Void;
            }
            else
            {
                return HubMethodType.Value;
            }
        }

        public virtual async Task<dynamic> InvokeAsync(PomeliumHub instance, MethodInfo methodInfo, object[] args)
        {
            dynamic ret;
            var method = methodInfo;
            var parameters = method.GetParameters();
            var newArgs = new List<object>();
            for (var i = 0; i < parameters.Count(); i++)
            {
                newArgs.Add(JsonConvert.DeserializeObject(JsonConvert.SerializeObject(args[i]), parameters[i].ParameterType));
            }
            ret = method.Invoke(instance, newArgs.ToArray());
            switch(GetReturnValueType(method))
            {
                case HubMethodType.Task:
                    await ret;
                    break;
                case HubMethodType.TaskWithReturnValue:
                    ret = await ret;
                    break;
                case HubMethodType.Value:
                case HubMethodType.Void:
                default:
                    break;
            }
            return ret;
        }
    }
}
