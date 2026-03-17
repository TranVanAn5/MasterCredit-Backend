using backend.DTOs;

namespace backend.Interfaces
{
    public interface IChatService
    {
        // ═══════════════════════════════════════════════════════════════
        //  CONVERSATION MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Tạo conversation mới cho user</summary>
        Task<ApiResponse<ChatConversationDto>> StartConversationAsync(int userId, StartChatConversationDto dto);

        /// <summary>Lấy danh sách conversations của user</summary>
        Task<ApiResponse<ChatConversationListResponseDto>> GetUserConversationsAsync(int userId);

        /// <summary>Lấy chi tiết conversation với tất cả messages</summary>
        Task<ApiResponse<ChatConversationDetailDto>> GetConversationDetailAsync(int userId, int conversationId);

        /// <summary>Đóng conversation</summary>
        Task<ApiResponse<bool>> CloseConversationAsync(int userId, int conversationId);

        // ═══════════════════════════════════════════════════════════════
        //  MESSAGE MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Gửi tin nhắn trong conversation</summary>
        Task<ApiResponse<ChatMessageResponseDto>> SendMessageAsync(int userId, int conversationId, SendChatMessageDto dto);

        /// <summary>Đánh dấu tin nhắn đã đọc</summary>
        Task<ApiResponse<bool>> MarkMessagesAsReadAsync(int userId, int conversationId);

        /// <summary>Lấy tin nhắn mới trong conversation (polling)</summary>
        Task<ApiResponse<List<ChatMessageDto>>> GetNewMessagesAsync(int userId, int conversationId, DateTime since);

        // ═══════════════════════════════════════════════════════════════
        //  CUSTOMER SERVICE BOT
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Generates automatic customer service response</summary>
        Task<CustomerServiceResponse> GenerateAutoResponseAsync(string userMessage, int userId);
    }
}