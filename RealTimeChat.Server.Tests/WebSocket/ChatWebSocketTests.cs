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

    private static async Task<string> ReceiveMessageAsync(System.Net.WebSockets.WebSocket ws)
    {
        var buffer = new byte[4096];
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;

        do
        {
            result = await ws.ReceiveAsync(buffer, default);
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
    public async Task WebSocket_ReceivesHistoryOnInit()
    {
        using var ws = await ConnectWebSocketAsync();

        // Send init request
        await SendMessageAsync(ws, new { Type = "init" });

        // Receive history response
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Messages);
        Assert.NotNull(response.Errors);
    }

    [Fact]
    public async Task WebSocket_CanSendMessage()
    {
        using var ws = await ConnectWebSocketAsync();

        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = "WSTestUser",
            Text = "WebSocket test message"
        };

        await SendMessageAsync(ws, request);

        // Should receive the broadcast message
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Messages);
        Assert.NotNull(response.Errors);
        Assert.Single(response.Messages);
        Assert.Empty(response.Errors); // Sender receives no errors for valid message
    }

    [Fact]
    public async Task WebSocket_RejectsMessageWithEmptyName()
    {
        using var ws = await ConnectWebSocketAsync();

        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = "",
            Text = "Test"
        };

        await SendMessageAsync(ws, request);

        // Should receive error response
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Errors);
        Assert.NotEmpty(response.Errors); // Should have validation error
        Assert.NotNull(response.Messages);
        Assert.Empty(response.Messages); // No message should be saved
    }

    [Fact]
    public async Task WebSocket_RejectsMessageWithEmptyText()
    {
        using var ws = await ConnectWebSocketAsync();

        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = "TestUser",
            Text = ""
        };

        await SendMessageAsync(ws, request);

        // Should receive error response
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Errors);
        Assert.NotEmpty(response.Errors); // Should have validation error
        Assert.NotNull(response.Messages);
        Assert.Empty(response.Messages); // No message should be saved
    }

    [Fact]
    public async Task WebSocket_RejectsMessageTooLong()
    {
        using var ws = await ConnectWebSocketAsync();

        // Get max message length from configuration (default is 1000)
        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = "TestUser",
            Text = new string('A', 1001) // Exceed limit
        };

        await SendMessageAsync(ws, request);

        // Should receive error response
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Errors);
        Assert.NotEmpty(response.Errors); // Should have validation error
        Assert.NotNull(response.Messages);
        Assert.Empty(response.Messages); // No message should be saved
    }

    [Fact]
    public async Task WebSocket_RejectsSenderNameTooLong()
    {
        using var ws = await ConnectWebSocketAsync();

        // Get max sender name length from configuration (default is 100)
        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = new string('A', 101), // Exceed limit
            Text = "Test message"
        };

        await SendMessageAsync(ws, request);

        // Should receive error response
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Errors);
        Assert.NotEmpty(response.Errors); // Should have validation error
        Assert.NotNull(response.Messages);
        Assert.Empty(response.Messages); // No message should be saved
    }

    [Fact]
    public async Task WebSocket_AcceptsMessageAtMaxLength()
    {
        using var ws = await ConnectWebSocketAsync();

        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = "TestUser",
            Text = new string('A', 1000) // Exactly at limit
        };

        await SendMessageAsync(ws, request);

        // Should receive the broadcast message
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Messages);
        Assert.NotNull(response.Errors);
        Assert.Single(response.Messages);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task WebSocket_AcceptsSenderNameAtMaxLength()
    {
        using var ws = await ConnectWebSocketAsync();

        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = new string('A', 100), // Exactly at limit
            Text = "Test message"
        };

        await SendMessageAsync(ws, request);

        // Should receive the broadcast message
        var json = await ReceiveMessageAsync(ws);
        var response = JsonSerializer.Deserialize<ChatResponse>(json);

        Assert.NotNull(response);
        Assert.NotNull(response.Messages);
        Assert.NotNull(response.Errors);
        Assert.Single(response.Messages);
        Assert.Empty(response.Errors);
    }

    [Fact]
    public async Task WebSocket_BroadcastsToMultipleClients()
    {
        using var ws1 = await ConnectWebSocketAsync();
        using var ws2 = await ConnectWebSocketAsync();

        var request = new SendMessageRequest
        {
            Type = "message",
            SenderName = "Broadcaster",
            Text = "Broadcast test"
        };

        await SendMessageAsync(ws1, request);

        // Both clients should receive the message
        var json1 = await ReceiveMessageAsync(ws1);
        var json2 = await ReceiveMessageAsync(ws2);

        var response1 = JsonSerializer.Deserialize<ChatResponse>(json1);
        var response2 = JsonSerializer.Deserialize<ChatResponse>(json2);

        Assert.NotNull(response1);
        Assert.NotNull(response2);

        // Both should have messages
        Assert.NotNull(response1.Messages);
        Assert.NotNull(response2.Messages);
        Assert.Single(response1.Messages);
        Assert.Single(response2.Messages);

        // Sender (ws1) has no errors, receiver (ws2) should also have no errors
        Assert.NotNull(response1.Errors);
        Assert.NotNull(response2.Errors);
        Assert.Empty(response1.Errors);
        Assert.Empty(response2.Errors);
    }
}
