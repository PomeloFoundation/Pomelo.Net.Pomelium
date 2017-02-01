namespace Pomelo.Net.Pomelium
{
    public enum PacketType
    {
        Request,
        Response,
        InitSession,
        Exception,
        Disconnect,
        NodeDisconnect,
        Forward,
        ForwardBack,
        Heartbeat
    }
}
