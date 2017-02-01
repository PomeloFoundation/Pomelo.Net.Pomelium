# Pomelium
Pomelium is a light weight RPC library on which based .NET Core and TCP makes developers are able to invoke remote methods. It even contains session functions which similar to the web development. Developers could build their projects neatly.

The following sample is showing you how to use Pomelium

Server side：
```c#
public class Program 
{
    public static void Main(string[] args)
    {
        var server = PomeliumServer.CreateServer();
        server.OnConnectedEvents += Server_OnConnectedEvents;
        server.OnDisconnectedEvents += Server_OnDisconnectedEvents;
        server.Start("127.0.0.1", 6000);
        Console.Read();
    }

    private static void Server_OnDisconnectedEvents(Server.Client.LocalClient obj)
    {
        Console.WriteLine(obj.SessionId + " Disconnected");
    }

    private static void Server_OnConnectedEvents(Server.Client.LocalClient obj)
    {
        Console.WriteLine(obj.SessionId + " Connected");
    }
}

public class TestHub : PomeliumHub
{
    public int TestMethod(int a, int b)
    {
        return a + b;
    }
}
```

Client side：
```c#
public static void Main(string[] args)
{
    Test().Wait();
    Console.Read();
}

public static async Task Test()
{
    var client = new PomeliumClient();
    await client.ConnectAsync("127.0.0.1", 6000);
    var ret = await client.Server["Test"].TestMethod(1, 1);
    Console.WriteLine(ret);
}
```
