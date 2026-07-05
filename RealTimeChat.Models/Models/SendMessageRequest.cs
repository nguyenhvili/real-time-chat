using System.ComponentModel.DataAnnotations;

namespace RealTimeChat.Models.Models;

public class SendMessageRequest
{
    public string? Type { get; set; }

    [MaxLength(100)]
    public string? SenderName { get; set; }

    public string? Text { get; set; }
}
