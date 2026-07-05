using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using RealTimeChat.Models.Models;

namespace RealTimeChat.Client.Services
{
    public class WebSocketChatService(string serverUrl) : IChatService
    {
        private ClientWebSocket? webSocket;

        public event EventHandler<Message>? MessageReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        public bool IsConnected => webSocket?.State == WebSocketState.Open;

        public async Task ConnectAsync()
        {
            try
            {
                webSocket = new ClientWebSocket();

                await webSocket.ConnectAsync(new Uri($"{serverUrl}/ws"), default);
                ConnectionStatusChanged?.Invoke(this, "Connected");

                _ = Task.Run(ReceiveMessagesAsync);
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Connection failed: {ex.Message}");
                throw;
            }
        }

        public async Task<List<Message>> InitializeMessages()
        {
            if (webSocket == null)
            {
                throw new InvalidOperationException("WebSocket is not initialized. Call ConnectAsync() first.");
            }

            // Send initialize request
            var initRequest = new SendMessageRequest
            {
                Type = "init",
                SenderName = "Client",
                Text = string.Empty
            };

            var json = JsonSerializer.Serialize(initRequest);
            var bytes = Encoding.UTF8.GetBytes(json);

            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, default);

            // WebSockets has a listener to receive messages, so we don't need to wait for a response here.
            return [];
        }

        public async Task SendMessageAsync(string senderName, string text)
        {
            if (webSocket?.State != WebSocketState.Open)
            {
                throw new InvalidOperationException("WebSocket is not connected");
            }

            var request = new SendMessageRequest
            {
                Type = "message",
                SenderName = senderName,
                Text = text
            };

            var json = JsonSerializer.Serialize(request);
            var bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, default);
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];

            try
            {
                while (webSocket?.State == WebSocketState.Open)
                {
                    var responseBuilder = new StringBuilder();
                    WebSocketReceiveResult result;

                    do
                    {
                        result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), default);
                        responseBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var json = responseBuilder.ToString();
                        var response = JsonSerializer.Deserialize<ChatResponse>(json);

                        if (response?.Messages != null)
                        {
                            foreach (var message in response.Messages)
                            {
                                MessageReceived?.Invoke(this, message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusChanged?.Invoke(this, $"Disconnected: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                ConnectionStatusChanged?.Invoke(this, "Disconnected");
            }

            webSocket?.Dispose();
        }
    }
}
