namespace RealTimeChat.Server.Configuration;

public class ChatSettings
{
    public const string SectionName = "ChatSettings";

    public int MaxConnectedClients { get; set; } = 20;
    public int MaxMessageLength { get; set; } = 1000;
    public int MaxSenderNameLength { get; set; } = 100;
    public int MaxCachedMessages { get; set; } = 50;
    public int WebSocketBufferSize { get; set; } = 4096;
}
