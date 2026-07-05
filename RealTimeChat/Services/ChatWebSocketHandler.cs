using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RealTimeChat.Models.Models;
using RealTimeChat.Server.Configuration;

namespace RealTimeChat.Server.Services;

public class ChatWebSocketHandler(IServiceScopeFactory scopeFactory, MessageProcessing messageProcessing, IOptions<ChatSettings> settings)
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
            await ProcessIncomingAsync(webSocket, json);
        }
    }

    private async Task ProcessIncomingAsync(WebSocket senderSocket, string json)
    {
        SendMessageRequest? request = JsonSerializer.Deserialize<SendMessageRequest>(json);

        if (request is null)
            return;

        var senderClient = _clients.Values.FirstOrDefault(c => c.Socket == senderSocket);
        if (senderClient == default)
            return;

        // If the connection is initial, instead send the message history to the sender only
        if (request.Type == "init")
        {
            var response = await messageProcessing.InitializeHistoryAsync(scopeFactory, CancellationToken.None);
            await SendJsonAsync(senderSocket, senderClient.WriteLock, response, CancellationToken.None);
        }
        // If the message type is message, then broadcast the message to all clients
        else if (request.Type == "message")
        {
            // Process and broadcast the message
            var response = await messageProcessing.ProcessMessageAsync(request, scopeFactory);
            if (response.Errors.Count != 0)
            {
                await SendJsonAsync(senderSocket, senderClient.WriteLock, response, CancellationToken.None);
            }
            await BroadcastAsync(senderSocket, response);
        }
    }

    private async Task BroadcastAsync(WebSocket senderSocket, ChatResponse response, bool excludeSender = true)
    {
        // Broadcast message to all clients (without errors)
        if (response.Messages.Count > 0)
        {
            var broadcastBytes = JsonSerializer.SerializeToUtf8Bytes(response);

            var broadcastTasks = _clients.Values
                .Where(c => c.Socket != senderSocket && excludeSender)
                .Select(c => SendBytesAsync(c.Socket, c.WriteLock, broadcastBytes));

            await Task.WhenAll(broadcastTasks);
        }

        // Send to sender (with errors if any)
        var senderClient = _clients.Values.FirstOrDefault(c => c.Socket == senderSocket);
        if (senderClient != default)
        {
            await SendJsonAsync(senderSocket, senderClient.WriteLock, response, CancellationToken.None);
        }
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
