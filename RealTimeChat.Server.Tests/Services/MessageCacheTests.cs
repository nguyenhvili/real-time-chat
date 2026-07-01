using Microsoft.Extensions.Options;
using RealTimeChat.Database.Models;
using RealTimeChat.Server.Configuration;
using RealTimeChat.Server.Services;
using Xunit;

namespace RealTimeChat.Server.Tests.Services;

public class MessageCacheTests
{
    private MessageCache CreateCache(int maxMessages = 50)
    {
        var settings = Options.Create(new ChatSettings { MaxCachedMessages = maxMessages });
        return new MessageCache(settings);
    }

    [Fact]
    public void IsInitialized_ReturnsFalse_WhenNotInitialized()
    {
        var cache = CreateCache();

        Assert.False(cache.IsInitialized);
    }

    [Fact]
    public void Initialize_SetsIsInitializedToTrue()
    {
        var cache = CreateCache();
        var messages = new List<MessageEntity>
        {
            new() { Id = 1, SenderName = "User1", Text = "Hello", Timestamp = DateTime.UtcNow }
        };

        cache.Initialize(messages);

        Assert.True(cache.IsInitialized);
    }

    [Fact]
    public void Initialize_StoresMessages()
    {
        var cache = CreateCache();
        var messages = new List<MessageEntity>
        {
            new() { Id = 1, SenderName = "User1", Text = "Hello", Timestamp = DateTime.UtcNow },
            new() { Id = 2, SenderName = "User2", Text = "World", Timestamp = DateTime.UtcNow.AddSeconds(1) }
        };

        cache.Initialize(messages);
        var result = cache.GetMessages();

        Assert.Equal(2, result.Count);
        Assert.Equal("User1", result[0].SenderName);
        Assert.Equal("User2", result[1].SenderName);
    }

    [Fact]
    public void AddMessage_AddsMessageToBeginning()
    {
        var cache = CreateCache();
        cache.Initialize(new List<MessageEntity>());

        var message = new MessageEntity { Id = 1, SenderName = "User1", Text = "Hello", Timestamp = DateTime.UtcNow };
        cache.AddMessage(message);

        var result = cache.GetMessages();
        Assert.Single(result);
        Assert.Equal("User1", result[0].SenderName);
    }

    [Fact]
    public void AddMessage_RemovesOldestMessage_WhenMaxReached()
    {
        var cache = CreateCache(maxMessages: 3);
        var messages = new List<MessageEntity>
        {
            new() { Id = 1, SenderName = "User1", Text = "Msg1", Timestamp = DateTime.UtcNow },
            new() { Id = 2, SenderName = "User2", Text = "Msg2", Timestamp = DateTime.UtcNow.AddSeconds(1) },
            new() { Id = 3, SenderName = "User3", Text = "Msg3", Timestamp = DateTime.UtcNow.AddSeconds(2) }
        };
        cache.Initialize(messages);

        var newMessage = new MessageEntity { Id = 4, SenderName = "User4", Text = "Msg4", Timestamp = DateTime.UtcNow.AddSeconds(3) };
        cache.AddMessage(newMessage);

        var result = cache.GetMessages();
        Assert.Equal(3, result.Count);
        Assert.Equal("User4", result[0].SenderName); // Newest at the beginning
        Assert.Equal("User2", result[2].SenderName); // User1 (oldest) should be removed
    }

    [Fact]
    public void GetMessages_ReturnsEmptyList_WhenNotInitialized()
    {
        var cache = CreateCache();

        var result = cache.GetMessages();

        Assert.Empty(result);
    }

    [Fact]
    public void Initialize_ClearsPreviousMessages()
    {
        var cache = CreateCache();
        cache.Initialize(new List<MessageEntity>
        {
            new() { Id = 1, SenderName = "User1", Text = "Hello", Timestamp = DateTime.UtcNow }
        });

        cache.Initialize(new List<MessageEntity>
        {
            new() { Id = 2, SenderName = "User2", Text = "World", Timestamp = DateTime.UtcNow }
        });

        var result = cache.GetMessages();
        Assert.Single(result);
        Assert.Equal("User2", result[0].SenderName);
    }
}
