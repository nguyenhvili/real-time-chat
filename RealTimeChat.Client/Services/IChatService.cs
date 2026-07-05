using RealTimeChat.Models.Models;

namespace RealTimeChat.Client.Services
{
    public interface IChatService
    {
        event EventHandler<Message>? MessageReceived;
        event EventHandler<string>? ConnectionStatusChanged;

        Task<List<Message>> InitializeMessages();
        Task SendMessageAsync(string senderName, string text);
        Task ConnectAsync();
        Task DisconnectAsync();
        bool IsConnected { get; }
    }
}
