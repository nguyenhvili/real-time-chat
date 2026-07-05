using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using RealTimeChat.Models.Models;
using Xunit;

namespace RealTimeChat.Server.Tests.Controllers;

public class MessagesControllerTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetMessages_ReturnsOk()
    {
        var response = await _client.GetAsync("/messages");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMessages_ReturnsMessagesArray()
    {
        var response = await _client.GetAsync("/messages");
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(chatResponse);
        Assert.NotNull(chatResponse.Messages);
        Assert.NotNull(chatResponse.Errors);
        Assert.Empty(chatResponse.Errors);
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

        // ASP.NET validation returns ValidationProblemDetails, not ChatResponse
        // Our custom validation in MessageProcessing returns ChatResponse
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content); // Just verify we got an error response
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

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
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

        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task SendMessage_ReturnsValidationError_WhenTextTooLong()
    {
        var request = new SendMessageRequest
        {
            SenderName = "TestUser",
            Text = new string('A', 1001) // Max is 1000
        };

        var response = await _client.PostAsJsonAsync("/messages", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>();
        Assert.NotNull(chatResponse);
        Assert.NotEmpty(chatResponse.Errors);
    }

    [Fact]
    public async Task SendMessage_AndGetMessages_ReturnsPostedMessage()
    {
        var client = factory.CreateClient();
        var request = new SendMessageRequest
        {
            SenderName = "IntegrationUser",
            Text = "Integration test message"
        };

        await client.PostAsJsonAsync("/messages", request);
        var getResponse = await client.GetAsync("/messages");
        var chatResponse = await getResponse.Content.ReadFromJsonAsync<ChatResponse>();

        Assert.NotNull(chatResponse);
        Assert.NotNull(chatResponse.Messages);
        Assert.Contains(chatResponse.Messages, m => m.SenderName == "IntegrationUser" && m.Text == "Integration test message");
    }
}
