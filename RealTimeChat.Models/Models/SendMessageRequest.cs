using System.ComponentModel.DataAnnotations;

namespace RealTimeChat.Models.Models;

public class SendMessageRequest
{
    public string? Type { get; set; }

    [MaxLength(100)]
    public required string SenderName { get; set; }

    public required string Text { get; set; }
}
