using System.ComponentModel.DataAnnotations;

namespace RealTimeChat.Database.Models;

public class MessageEntity
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(100)]
    public string SenderName { get; set; } = string.Empty;

    [Required]
    public string Text { get; set; } = string.Empty;
}
