namespace RealTimeChat.Client.Models
{
    public class AppSettings
    {
        public string Username { get; set; } = string.Empty;
        public bool UseWebSocket { get; set; } = true;
        public string ServerUrl { get; set; } = "wss://localhost:7269";
    }
}
