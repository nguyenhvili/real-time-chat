using RealTimeChat.Database.Models;
using RealTimeChat.Models.Models;

namespace RealTimeChat.Server.Services
{
    public class MessageMapper
    {
        public IEnumerable<Message> ToModel(IEnumerable<MessageEntity> messages)
        {
            return messages.Select(ToModel);
        }

        public Message ToModel(MessageEntity message)
        {
            return new Message
            {
                Timestamp = message.Timestamp,
                SenderName = message.SenderName,
                Text = message.Text
            };
        }

        public MessageEntity ToEntity(SendMessageRequest request)
        {
            return new MessageEntity
            {
                Timestamp = DateTime.UtcNow,
                SenderName = request.SenderName!,
                Text = request.Text!
            };
        }
    }
}
