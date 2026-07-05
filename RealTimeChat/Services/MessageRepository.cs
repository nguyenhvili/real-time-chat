using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RealTimeChat.Database;
using RealTimeChat.Database.Models;
using RealTimeChat.Server.Configuration;

namespace RealTimeChat.Server.Services;

public class MessageRepository(ChatDbContext db, IOptions<ChatSettings> settings)
{
    private readonly int _maxMessages = settings.Value.MaxCachedMessages;

    public async Task<List<MessageEntity>> GetRecentMessagesAsync(CancellationToken cancellationToken = default)
    {
        return await db.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(_maxMessages)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
