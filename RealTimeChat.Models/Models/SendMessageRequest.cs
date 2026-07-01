using System.ComponentModel.DataAnnotations;

namespace RealTimeChat.Models.Models;

public class SendMessageRequest
{
    [Required]
    [MaxLength(100)]
    public required string SenderName { get; set; }

    [Required]
    public required string Text { get; set; }
}
