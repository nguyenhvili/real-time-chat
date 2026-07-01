using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RealTimeChat.Models.Models;
using Xunit;

namespace RealTimeChat.Server.Tests.WebSocket;

public class ChatWebSocketTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private async Task<System.Net.WebSockets.WebSocket> ConnectWebSocketAsync()
    {
        var wsClient = factory.Server.CreateWebSocketClient();
        var wsUri = new Uri(factory.Server.BaseAddress, "ws");

        return await wsClient.ConnectAsync(wsUri, CancellationToken.None);
    }

    private static async Task<string> ReceiveMessageAsync(System.Net.WebSockets.WebSocket ws, CancellationToken ct = default)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(buffer, ct);
            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static async Task SendMessageAsync(System.Net.WebSockets.WebSocket ws, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    [Fact]
    public async Task WebSocket_CanConnect()
    {
        using var ws = await ConnectWebSocketAsync();

        Assert.Equal(WebSocketState.Open, ws.State);
    }

    [Fact]
    public async Task WebSocket_ReceivesHistoryOnConnect()
    {
        using var ws = await ConnectWebSocketAsync();

        var message = await ReceiveMessageAsync(ws);
        var data = JsonDocument.Parse(message);

        Assert.True(data.RootElement.TryGetProperty("type", out var type));
        Assert.Equal("history", type.GetString());
    }

    [Fact]
    public async Task WebSocket_CanSendMessage()
    {
        using var ws = await ConnectWebSocketAsync();

        // Receive history first
        await ReceiveMessageAsync(ws);

        var request = new SendMessageRequest
        {
            SenderName = "WSTestUser",
            Text = "WebSocket test message"
        };

        await SendMessageAsync(ws, request);

        // Should receive the broadcast message
        var response = await ReceiveMessageAsync(ws, new CancellationTokenSource(5000).Token);
        var data = JsonDocument.Parse(response);

        Assert.True(data.RootElement.TryGetProperty("type", out var type));
        Assert.Equal("message", type.GetString());
    }

    [Fact]
    public async Task WebSocket_RejectsMessageWithEmptyName()
    {
        using var ws = await ConnectWebSocketAsync();
        await ReceiveMessageAsync(ws); // Receive history

        var request = new SendMessageRequest
        {
            SenderName = "",
            Text = "Test"
        };

        await SendMessageAsync(ws, request);

        // Should not receive any response (message silently dropped)
        var cts = new CancellationTokenSource(1000);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ReceiveMessageAsync(ws, cts.Token);
        });
    }

    [Fact]
    public async Task WebSocket_RejectsMessageWithEmptyText()
    {
        using var ws = await ConnectWebSocketAsync();
        await ReceiveMessageAsync(ws); // Receive history

        var request = new SendMessageRequest
        {
            SenderName = "TestUser",
            Text = ""
        };

        await SendMessageAsync(ws, request);

        // Should not receive any response (message silently dropped)
        var cts = new CancellationTokenSource(1000);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ReceiveMessageAsync(ws, cts.Token);
        });
    }

    [Fact]
    public async Task WebSocket_RejectsMessageTooLong()
    {
        using var ws = await ConnectWebSocketAsync();
        await ReceiveMessageAsync(ws); // Receive history

        // Get max message length from configuration (default is 1000)
        var request = new SendMessageRequest
        {
            SenderName = "TestUser",
            Text = new string('A', 1001) // Exceed limit
        };

        await SendMessageAsync(ws, request);

        // Should not receive any response (message silently dropped)
        var cts = new CancellationTokenSource(1000);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ReceiveMessageAsync(ws, cts.Token);
        });
    }

    [Fact]
    public async Task WebSocket_RejectsSenderNameTooLong()
    {
        using var ws = await ConnectWebSocketAsync();
        await ReceiveMessageAsync(ws); // Receive history

        // Get max sender name length from configuration (default is 100)
        var request = new SendMessageRequest
        {
            SenderName = new string('A', 101), // Exceed limit
            Text = "Test message"
        };

        await SendMessageAsync(ws, request);

        // Should not receive any response (message silently dropped)
        var cts = new CancellationTokenSource(1000);
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ReceiveMessageAsync(ws, cts.Token);
        });
    }

    [Fact]
    public async Task WebSocket_AcceptsMessageAtMaxLength()
    {
        using var ws = await ConnectWebSocketAsync();
        await ReceiveMessageAsync(ws); // Receive history

        var request = new SendMessageRequest
        {
            SenderName = "TestUser",
            Text = new string('A', 1000) // Exactly at limit
        };

        await SendMessageAsync(ws, request);

        // Should receive the broadcast message
        var response = await ReceiveMessageAsync(ws, new CancellationTokenSource(5000).Token);
        var data = JsonDocument.Parse(response);

        Assert.True(data.RootElement.TryGetProperty("type", out var type));
        Assert.Equal("message", type.GetString());
    }

    [Fact]
    public async Task WebSocket_AcceptsSenderNameAtMaxLength()
    {
        using var ws = await ConnectWebSocketAsync();
        await ReceiveMessageAsync(ws); // Receive history

        var request = new SendMessageRequest
        {
            SenderName = new string('A', 100), // Exactly at limit
            Text = "Test message"
        };

        await SendMessageAsync(ws, request);

        // Should receive the broadcast message
        var response = await ReceiveMessageAsync(ws, new CancellationTokenSource(5000).Token);
        var data = JsonDocument.Parse(response);

        Assert.True(data.RootElement.TryGetProperty("type", out var type));
        Assert.Equal("message", type.GetString());
    }

    [Fact]
    public async Task WebSocket_BroadcastsToMultipleClients()
    {
        using var ws1 = await ConnectWebSocketAsync();
        using var ws2 = await ConnectWebSocketAsync();

        await ReceiveMessageAsync(ws1); // History for ws1
        await ReceiveMessageAsync(ws2); // History for ws2

        var request = new SendMessageRequest
        {
            SenderName = "Broadcaster",
            Text = "Broadcast test"
        };

        await SendMessageAsync(ws1, request);

        // Both clients should receive the message
        var response1 = await ReceiveMessageAsync(ws1, new CancellationTokenSource(5000).Token);
        var response2 = await ReceiveMessageAsync(ws2, new CancellationTokenSource(5000).Token);

        var data1 = JsonDocument.Parse(response1);
        var data2 = JsonDocument.Parse(response2);

        Assert.Equal("message", data1.RootElement.GetProperty("type").GetString());
        Assert.Equal("message", data2.RootElement.GetProperty("type").GetString());
    }
}
