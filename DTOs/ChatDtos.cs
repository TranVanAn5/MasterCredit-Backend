using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    // ═══════════════════════════════════════════════════════════════
    //  CHAT CONVERSATION DTOs
    // ═══════════════════════════════════════════════════════════════

    public class StartChatConversationDto
    {
        [Required(ErrorMessage = "Chủ đề cuộc hội thoại là bắt buộc.")]
        [StringLength(200, ErrorMessage = "Chủ đề không được vượt quá 200 ký tự.")]
        public string Subject { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Tin nhắn đầu tiên không được vượt quá 500 ký tự.")]
        public string? InitialMessage { get; set; }
    }

    public class ChatConversationDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? AssignedAgentName { get; set; }
        public int UnreadCount { get; set; }
        public ChatMessageDto? LastMessage { get; set; }
    }

    public class ChatConversationDetailDto
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? AssignedAgentName { get; set; }
        public List<ChatMessageDto> Messages { get; set; } = new();
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHAT MESSAGE DTOs
    // ═══════════════════════════════════════════════════════════════

    public class SendChatMessageDto
    {
        [Required(ErrorMessage = "Nội dung tin nhắn là bắt buộc.")]
        [StringLength(2000, ErrorMessage = "Tin nhắn không được vượt quá 2000 ký tự.")]
        public string Content { get; set; } = string.Empty;

        public string MessageType { get; set; } = "Text";
    }

    public class ChatMessageDto
    {
        public int Id { get; set; }
        public int ConversationId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string SenderType { get; set; } = string.Empty; // Customer, Agent, System
        public string SenderName { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public string? AttachmentUrl { get; set; }
        public string MessageType { get; set; } = "Text";
    }

    // ═══════════════════════════════════════════════════════════════
    //  CHAT RESPONSE DTOs
    // ═══════════════════════════════════════════════════════════════

    public class ChatConversationListResponseDto
    {
        public List<ChatConversationDto> Conversations { get; set; } = new();
        public int TotalUnreadMessages { get; set; }
        public int ActiveConversationsCount { get; set; }
    }

    public class ChatMessageResponseDto
    {
        public ChatMessageDto Message { get; set; } = new();
        public bool HasAutoResponse { get; set; }
        public ChatMessageDto? AutoResponse { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════
    //  CUSTOMER SERVICE BOT RESPONSE TEMPLATES
    // ═══════════════════════════════════════════════════════════════

    public class CustomerServiceResponse
    {
        public string Content { get; set; } = string.Empty;
        public string ResponseType { get; set; } = "Auto"; // Auto, Manual, Escalated
        public bool RequiresHumanAgent { get; set; }
        public List<string> SuggestedActions { get; set; } = new();
    }
}