using RealTimeChat.Database;
using RealTimeChat.Models.Models;
using Microsoft.Extensions.Options;
using RealTimeChat.Server.Configuration;

namespace RealTimeChat.Server.Services;

public class MessageProcessing(MessageCache cache, MessageMapper mapper, IOptions<ChatSettings> settings)
{
    private readonly ChatSettings _settings = settings.Value;

    public async Task<ChatResponse> ProcessMessageAsync(SendMessageRequest request, IServiceScopeFactory serviceScopeFactory)
    {
        var response = new ChatResponse();

        // Validate request
        if (request is null || string.IsNullOrWhiteSpace(request.SenderName) || string.IsNullOrWhiteSpace(request.Text))
        {
            response.Errors.Add("Invalid message: sender name and text are required");
            return response;
        }

        // Check message length limits
        if (request.Text.Length > _settings.MaxMessageLength)
        {
            response.Errors.Add($"Message text exceeds maximum length of {_settings.MaxMessageLength} characters");
            return response;
        }

        // Check sender name length limits
        if (request.SenderName.Length > _settings.MaxSenderNameLength)
        {
            response.Errors.Add($"Sender name exceeds maximum length of {_settings.MaxSenderNameLength} characters");
            return response;
        }

        // Create and save message
        var message = mapper.ToEntity(request);

        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        dbContext.Messages.Add(message);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        cache.AddMessage(message);
        response.Messages.Add(mapper.ToModel(message));

        return response;
    }

    public ChatResponse GetHistory()
    {
        var response = new ChatResponse
        {
            Messages = [.. mapper.ToModel(cache.GetMessages())],
        };
        return response;
    }

    public async Task<ChatResponse> InitializeHistoryAsync(IServiceScopeFactory serviceScopeFactory, CancellationToken cancellationToken = default)
    {
        if (!cache.IsInitialized)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<MessageRepository>();
            var messages = await repository.GetRecentMessagesAsync(cancellationToken);
            cache.Initialize(messages);
        }

        return GetHistory();
    }
}
