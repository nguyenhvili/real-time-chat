using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RealTimeChat.Database;
using RealTimeChat.Database.Models;
using RealTimeChat.Server.Configuration;

namespace RealTimeChat.Server.Services;

public class MessageRepository(ChatDbContext db, IOptions<ChatSettings> settings)
{
    private readonly int _maxMessages = settings.Value.MaxCachedMessages;

    public async Task<List<MessageEntity>> GetRecentMessagesAsync()
    {
        return await db.Messages
            .OrderBy(m => m.Timestamp)
            .Take(_maxMessages)
            .ToListAsync();
    }
}
