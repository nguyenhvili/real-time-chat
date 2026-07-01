using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealTimeChat.Database;
using RealTimeChat.Models.Models;
using RealTimeChat.Server.Configuration;

namespace RealTimeChat.Server.Services;

public class ChatWebSocketHandler(IServiceScopeFactory scopeFactory, MessageCache cache, MessageMapper mapper, IOptions<ChatSettings> settings)
{
    private readonly ConcurrentDictionary<Guid, (WebSocket Socket, SemaphoreSlim WriteLock)> _clients = new();
    private readonly ChatSettings _settings = settings.Value;

    public async Task HandleAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        // Check if max clients limit is reached
        if (_clients.Count >= _settings.MaxConnectedClients)
        {
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Maximum client limit reached", cancellationToken);
            return;
        }

        var id = Guid.NewGuid();
        var writeLock = new SemaphoreSlim(1, 1);
        _clients[id] = (webSocket, writeLock);

        try
        {
            await SendHistoryAsync(webSocket, writeLock, cancellationToken);
            await ReceiveLoopAsync(webSocket, cancellationToken);
        }
        finally
        {
            _clients.TryRemove(id, out _);
            writeLock.Dispose();
            if (webSocket.State == WebSocketState.Open)
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
        }
    }

    private async Task SendHistoryAsync(WebSocket webSocket, SemaphoreSlim writeLock, CancellationToken cancellationToken)
    {
        if (!cache.IsInitialized)
        {
            using var scope = scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<MessageRepository>();
            var messages = await repository.GetRecentMessagesAsync(cancellationToken);
            cache.Initialize(messages);
        }

        await SendJsonAsync(webSocket, writeLock, new { type = "history", messages = cache.GetMessages() }, cancellationToken);
    }

    private async Task ReceiveLoopAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var buffer = new byte[_settings.WebSocketBufferSize];
        using var ms = new MemoryStream();

        while (webSocket.State == WebSocketState.Open)
        {
            ms.SetLength(0);
            WebSocketReceiveResult result;

            do
            {
                result = await webSocket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                    return;
                ms.Write(buffer, 0, result.Count);
            } while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            var json = Encoding.UTF8.GetString(ms.ToArray());
            await ProcessIncomingAsync(json);
        }
    }

    private async Task ProcessIncomingAsync(string json)
    {
        SendMessageRequest? request = JsonSerializer.Deserialize<SendMessageRequest>(json);

        if (request is null || string.IsNullOrWhiteSpace(request.SenderName) || string.IsNullOrWhiteSpace(request.Text))
            return;

        // Check message and sender name length limits
        if (request.Text.Length > _settings.MaxMessageLength || request.SenderName.Length > _settings.MaxSenderNameLength)
            return;

        var message = mapper.ToEntity(request);

        using (var scope = scopeFactory.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
            dbContext.Messages.Add(message);
            await dbContext.SaveChangesAsync(CancellationToken.None);
        }

        cache.AddMessage(message);

        await BroadcastAsync(new { type = "message", message });
    }

    private async Task BroadcastAsync(object payload)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        var tasks = _clients.Values.Select(c => SendBytesAsync(c.Socket, c.WriteLock, bytes));
        await Task.WhenAll(tasks);
    }

    private static async Task SendJsonAsync(WebSocket webSocket, SemaphoreSlim writeLock, object payload, CancellationToken cancellationToken)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payload);
        await SendBytesAsync(webSocket, writeLock, bytes, cancellationToken);
    }

    private static async Task SendBytesAsync(WebSocket webSocket, SemaphoreSlim writeLock, byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (webSocket.State != WebSocketState.Open)
            return;

        await writeLock.WaitAsync(CancellationToken.None);
        try
        {
            if (webSocket.State == WebSocketState.Open)
                await webSocket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
        }
        catch (WebSocketException)
        {
            // Connection dropped; HandleAsync will remove this client from the dictionary
        }
        finally
        {
            writeLock.Release();
        }
    }
}
