using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; }
        [ForeignKey("ConversationId")]
        public ChatConversation Conversation { get; set; } = null!;

        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(20)]
        public string SenderType { get; set; } = string.Empty; // Customer, Agent, System

        public int? SenderId { get; set; } // User ID hoặc Agent ID

        [MaxLength(100)]
        public string? SenderName { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Để mở rộng: attachment, message type, etc.
        [MaxLength(500)]
        public string? AttachmentUrl { get; set; }

        [MaxLength(50)]
        public string MessageType { get; set; } = "Text"; // Text, Image, File, System
    }
}