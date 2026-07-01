using Microsoft.AspNetCore.Mvc;
using RealTimeChat.Database;
using RealTimeChat.Database.Models;
using RealTimeChat.Models.Models;
using RealTimeChat.Server.Services;

namespace RealTimeChat.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class MessagesController(ChatDbContext db, MessageCache cache, MessageMapper mapper, MessageRepository repository) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Message>>> GetMessages()
    {
        if (!cache.IsInitialized)
        {
            var messages = await repository.GetRecentMessagesAsync();
            cache.Initialize(messages);
        }

        return Ok(mapper.ToModel(cache.GetMessages()));
    }

    [HttpPost]
    public async Task<ActionResult<Message>> SendMessage([FromBody] SendMessageRequest request)
    {
        var message = new MessageEntity
        {
            Timestamp = DateTime.UtcNow,
            SenderName = request.SenderName,
            Text = request.Text
        };

        db.Messages.Add(message);
        await db.SaveChangesAsync();

        cache.AddMessage(message);

        return CreatedAtAction(nameof(GetMessages), message);
    }
}
