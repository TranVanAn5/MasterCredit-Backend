using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models
{
    public class ChatConversation
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;

        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty; // Chủ đề cuộc trò chuyện

        [MaxLength(20)]
        public string Status { get; set; } = "Active"; // Active, Closed, Waiting

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastMessageAt { get; set; }

        // Customer service agent ID (nullable for AI/auto responses)
        public int? AssignedAgentId { get; set; }

        [MaxLength(100)]
        public string? AssignedAgentName { get; set; }

        // Collection of messages
        public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}