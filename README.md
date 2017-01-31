# Pomelium
Pomelium is a light weight RPC library on which based .NET Core and TCP makes developers are able to invoke remote methods. It even contains session functions which similar to the web development. Developers could build their projects neatly.

The following sample is showing you how to use Pomelium

Server side：
```c#
public class Program
{
    public static void Main(string[] args)
    {
        var server = new PomeliumServer();
        server.Start("127.0.0.1", 6000);
        Console.Read();
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
    var client = new PomeliumClient("127.0.0.1", 6000);
    await client.ConnectAsync();
    var ret = await client.Server["Test"].TestMethod(1, 1);
    Console.WriteLine(ret);
}
```
