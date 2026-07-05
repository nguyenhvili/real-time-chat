using System.ComponentModel.DataAnnotations;

namespace RealTimeChat.Models.Models
{
    public class Message
    {
        public DateTime Timestamp { get; set; }

        [Required]
        [MaxLength(100)]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;
    }
}
