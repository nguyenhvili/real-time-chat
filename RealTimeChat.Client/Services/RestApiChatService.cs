using System.Net.Http;
using System.Net.Http.Json;
using RealTimeChat.Models.Models;

namespace RealTimeChat.Client.Services
{
    public class RestApiChatService : IChatService
    {
        private readonly HttpClient _httpClient;
        private Timer? _pollingTimer;
        private DateTime _lastMessageTime = DateTime.MinValue;

        public event EventHandler<Message>? MessageReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        public bool IsConnected { get; private set; }

        public RestApiChatService(string serverUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(serverUrl)
            };
        }

        public async Task ConnectAsync()
        {
            IsConnected = true;
            ConnectionStatusChanged?.Invoke(this, "Connected via REST API");

            // Start polling for new messages every 1 second
            _pollingTimer = new Timer(async _ => await PollForNewMessages(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public async Task<List<Message>> InitializeMessages()
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<ChatResponse>("Messages");

                if (response?.Messages != null && response.Messages.Count > 0)
                {
                    _lastMessageTime = response.Messages.Max(m => m.Timestamp);
                    return response.Messages;
                }

                return [];
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Failed to get messages: {ex.Message}");
                return [];
            }
        }

        public async Task SendMessageAsync(string senderName, string text)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Not connected to the API");
            }

            try
            {
                var request = new SendMessageRequest
                {
                    SenderName = senderName,
                    Text = text
                };

                var response = await _httpClient.PostAsJsonAsync("Messages", request);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Failed to send message: {ex.Message}");
                throw;
            }
        }

        private async Task PollForNewMessages()
        {
            if (!IsConnected) return;

            try
            {
                var response = await _httpClient.GetFromJsonAsync<ChatResponse>("Messages");

                if (response?.Messages != null)
                {
                    var newMessages = response.Messages.Where(m => m.Timestamp > _lastMessageTime).ToList();

                    foreach (var message in newMessages)
                    {
                        MessageReceived?.Invoke(this, message);
                        _lastMessageTime = message.Timestamp;
                    }
                }
            }
            catch
            {
                // Ignore polling errors to avoid spamming the user
                // TODO: add logging
            }
        }

        public async Task DisconnectAsync()
        {
            IsConnected = false;
            _pollingTimer?.Dispose();
            _pollingTimer = null;
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
        }
    }
}
