using System.Reflection;
using System.Threading.Tasks;
using Pomelo.Net.Pomelium.Server.Client;

namespace Pomelo.Net.Pomelium.Server.HubActivitor
{
    public enum HubMethodType
    {
        Void,
        Task,
        TaskWithReturnValue,
        Value
    }

    public interface IHubActivitor
    {
        PomeliumHub CreateInstance(string hubName, IClient client, Packet request);
        
        Task<dynamic> InvokeAsync(PomeliumHub instance, MethodInfo methodInfo, object[] args);

        MethodInfo GetMethod(PomeliumHub instance, string methodName);

        HubMethodType GetReturnValueType(MethodInfo methodInfo);
    }
}
