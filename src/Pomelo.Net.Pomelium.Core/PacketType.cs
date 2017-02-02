namespace Pomelo.Net.Pomelium
{
    public enum PacketType
    {
        Request,
        Response,
        Exception,
        Disconnect,
        NodeDisconnect,
        Forward,
        ForwardBack,
        Heartbeat
    }
}
