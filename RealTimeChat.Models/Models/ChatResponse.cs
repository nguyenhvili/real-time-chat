namespace RealTimeChat.Models.Models;

public class ChatResponse
{
    public List<Message> Messages { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
