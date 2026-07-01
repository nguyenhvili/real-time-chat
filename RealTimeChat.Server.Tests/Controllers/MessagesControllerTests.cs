using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RealTimeChat.Models.Models;
using Xunit;

namespace RealTimeChat.Server.Tests.Controllers;

public class MessagesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public MessagesControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMessages_ReturnsOk()
    {
        var response = await _client.GetAsync("/messages");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsEmptyArray_Initially()
    {
        var response = await _client.GetAsync("/messages");
        var messages = await response.Content.ReadFromJsonAsync<Message[]>();

        Assert.NotNull(messages);
        Assert.Empty(messages);
    }

    [Fact]
    public async Task SendMessage_ReturnsCreated_WithValidMessage()
    {
        var request = new SendMessageRequest
        {
            SenderName = "TestUser",
            Text = "Hello, World!"
        };

        var response = await _client.PostAsJsonAsync("/messages", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsValidationError_WithEmptyName()
    {
        var request = new SendMessageRequest
        {
            SenderName = "",
            Text = "Hello"
        };

        var response = await _client.PostAsJsonAsync("/messages", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsValidationError_WithEmptyText()
    {
        var request = new SendMessageRequest
        {
            SenderName = "TestUser",
            Text = ""
        };

        var response = await _client.PostAsJsonAsync("/messages", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ReturnsValidationError_WhenNameTooLong()
    {
        var request = new SendMessageRequest
        {
            SenderName = new string('A', 101), // Max is 100
            Text = "Hello"
        };

        var response = await _client.PostAsJsonAsync("/messages", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SendMessage_AndGetMessages_ReturnsPostedMessage()
    {
        var client = _factory.CreateClient();
        var request = new SendMessageRequest
        {
            SenderName = "IntegrationUser",
            Text = "Integration test message"
        };

        await client.PostAsJsonAsync("/messages", request);
        var getResponse = await client.GetAsync("/messages");
        var messages = await getResponse.Content.ReadFromJsonAsync<Message[]>();

        Assert.NotNull(messages);
        Assert.Contains(messages, m => m.SenderName == "IntegrationUser" && m.Text == "Integration test message");
    }
}
