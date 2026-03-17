using System.Text.RegularExpressions;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext _db;

        public ChatService(AppDbContext db)
        {
            _db = db;
        }

        // ═══════════════════════════════════════════════════════════════
        //  CONVERSATION MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        public async Task<ApiResponse<ChatConversationDto>> StartConversationAsync(int userId, StartChatConversationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Subject))
                return ApiResponse<ChatConversationDto>.Fail("Chủ đề cuộc hội thoại là bắt buộc.");

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // Create conversation
                var conversation = new ChatConversation
                {
                    UserId = userId,
                    Subject = dto.Subject.Trim(),
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    LastMessageAt = DateTime.UtcNow,
                    AssignedAgentName = "AI Assistant"
                };

                _db.ChatConversations.Add(conversation);
                await _db.SaveChangesAsync();

                // Add initial welcome message from system
                var welcomeMessage = new ChatMessage
                {
                    ConversationId = conversation.Id,
                    Content = $"Chào mừng bạn đến với hệ thống hỗ trợ khách hàng MasterCredit! Tôi là AI Assistant và sẽ hỗ trợ bạn về: {dto.Subject}. Hãy mô tả chi tiết vấn đề của bạn.",
                    SenderType = "System",
                    SenderName = "AI Assistant",
                    MessageType = "Text",
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _db.ChatMessages.Add(welcomeMessage);

                // Add initial user message if provided
                if (!string.IsNullOrWhiteSpace(dto.InitialMessage))
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    var userMessage = new ChatMessage
                    {
                        ConversationId = conversation.Id,
                        Content = dto.InitialMessage.Trim(),
                        SenderType = "Customer",
                        SenderId = userId,
                        SenderName = user?.Name ?? "Khách hàng",
                        MessageType = "Text",
                        SentAt = DateTime.UtcNow,
                        IsRead = false
                    };

                    _db.ChatMessages.Add(userMessage);

                    // Generate AI response
                    var aiResponse = await GenerateAutoResponseAsync(dto.InitialMessage, userId);
                    if (!string.IsNullOrEmpty(aiResponse.Content))
                    {
                        var autoReply = new ChatMessage
                        {
                            ConversationId = conversation.Id,
                            Content = aiResponse.Content,
                            SenderType = aiResponse.RequiresHumanAgent ? "Agent" : "System",
                            SenderName = "AI Assistant",
                            MessageType = "Text",
                            SentAt = DateTime.UtcNow.AddSeconds(2), // Slight delay for realism
                            IsRead = false
                        };

                        _db.ChatMessages.Add(autoReply);
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var result = new ChatConversationDto
                {
                    Id = conversation.Id,
                    Subject = conversation.Subject,
                    Status = conversation.Status,
                    CreatedAt = conversation.CreatedAt,
                    LastMessageAt = conversation.LastMessageAt,
                    AssignedAgentName = conversation.AssignedAgentName,
                    UnreadCount = await _db.ChatMessages
                        .CountAsync(m => m.ConversationId == conversation.Id && !m.IsRead && m.SenderType != "Customer")
                };

                return ApiResponse<ChatConversationDto>.Ok("Cuộc hội thoại đã được tạo thành công.", result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<ChatConversationDto>.Fail($"Lỗi khi tạo cuộc hội thoại: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ChatConversationListResponseDto>> GetUserConversationsAsync(int userId)
        {
            var conversations = await _db.ChatConversations
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Select(c => new ChatConversationDto
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    Status = c.Status,
                    CreatedAt = c.CreatedAt,
                    LastMessageAt = c.LastMessageAt,
                    AssignedAgentName = c.AssignedAgentName,
                    UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderType != "Customer"),
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.SentAt)
                        .Select(m => new ChatMessageDto
                        {
                            Id = m.Id,
                            Content = m.Content.Length > 100 ? m.Content.Substring(0, 100) + "..." : m.Content,
                            SenderType = m.SenderType,
                            SenderName = m.SenderName ?? "",
                            SentAt = m.SentAt,
                            MessageType = m.MessageType
                        })
                        .FirstOrDefault()
                })
                .ToListAsync();

            var totalUnread = conversations.Sum(c => c.UnreadCount);
            var activeCount = conversations.Count(c => c.Status == "Active");

            var result = new ChatConversationListResponseDto
            {
                Conversations = conversations,
                TotalUnreadMessages = totalUnread,
                ActiveConversationsCount = activeCount
            };

            return ApiResponse<ChatConversationListResponseDto>.Ok("Lấy danh sách cuộc hội thoại thành công.", result);
        }

        public async Task<ApiResponse<ChatConversationDetailDto>> GetConversationDetailAsync(int userId, int conversationId)
        {
            var conversation = await _db.ChatConversations
                .Include(c => c.Messages)
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null)
                return ApiResponse<ChatConversationDetailDto>.Fail("Không tìm thấy cuộc hội thoại.");

            var messages = conversation.Messages
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    Content = m.Content,
                    SenderType = m.SenderType,
                    SenderName = m.SenderName ?? "",
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                    AttachmentUrl = m.AttachmentUrl,
                    MessageType = m.MessageType
                })
                .ToList();

            var result = new ChatConversationDetailDto
            {
                Id = conversation.Id,
                Subject = conversation.Subject,
                Status = conversation.Status,
                CreatedAt = conversation.CreatedAt,
                LastMessageAt = conversation.LastMessageAt,
                AssignedAgentName = conversation.AssignedAgentName,
                Messages = messages
            };

            return ApiResponse<ChatConversationDetailDto>.Ok("Lấy chi tiết cuộc hội thoại thành công.", result);
        }

        public async Task<ApiResponse<bool>> CloseConversationAsync(int userId, int conversationId)
        {
            var conversation = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null)
                return ApiResponse<bool>.Fail("Không tìm thấy cuộc hội thoại.");

            if (conversation.Status == "Closed")
                return ApiResponse<bool>.Fail("Cuộc hội thoại đã được đóng.");

            conversation.Status = "Closed";

            // Add system message
            var systemMessage = new ChatMessage
            {
                ConversationId = conversationId,
                Content = "Cuộc hội thoại đã được đóng. Cảm ơn bạn đã sử dụng dịch vụ hỗ trợ của MasterCredit.",
                SenderType = "System",
                SenderName = "Hệ thống",
                MessageType = "System",
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _db.ChatMessages.Add(systemMessage);
            await _db.SaveChangesAsync();

            return ApiResponse<bool>.Ok("Cuộc hội thoại đã được đóng thành công.", true);
        }

        // ═══════════════════════════════════════════════════════════════
        //  MESSAGE MANAGEMENT
        // ═══════════════════════════════════════════════════════════════

        public async Task<ApiResponse<ChatMessageResponseDto>> SendMessageAsync(int userId, int conversationId, SendChatMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
                return ApiResponse<ChatMessageResponseDto>.Fail("Nội dung tin nhắn không được để trống.");

            var conversation = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null)
                return ApiResponse<ChatMessageResponseDto>.Fail("Không tìm thấy cuộc hội thoại.");

            if (conversation.Status == "Closed")
                return ApiResponse<ChatMessageResponseDto>.Fail("Cuộc hội thoại đã được đóng.");

            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                // Create user message
                var userMessage = new ChatMessage
                {
                    ConversationId = conversationId,
                    Content = dto.Content.Trim(),
                    SenderType = "Customer",
                    SenderId = userId,
                    SenderName = user?.Name ?? "Khách hàng",
                    MessageType = dto.MessageType,
                    SentAt = DateTime.UtcNow,
                    IsRead = false
                };

                _db.ChatMessages.Add(userMessage);

                // Update conversation
                conversation.LastMessageAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                var userMessageDto = new ChatMessageDto
                {
                    Id = userMessage.Id,
                    ConversationId = userMessage.ConversationId,
                    Content = userMessage.Content,
                    SenderType = userMessage.SenderType,
                    SenderName = userMessage.SenderName ?? "",
                    SentAt = userMessage.SentAt,
                    IsRead = userMessage.IsRead,
                    MessageType = userMessage.MessageType
                };

                // Generate AI auto-response
                var aiResponse = await GenerateAutoResponseAsync(dto.Content, userId);
                ChatMessageDto? autoResponseDto = null;

                if (!string.IsNullOrEmpty(aiResponse.Content))
                {
                    var autoMessage = new ChatMessage
                    {
                        ConversationId = conversationId,
                        Content = aiResponse.Content,
                        SenderType = aiResponse.RequiresHumanAgent ? "Agent" : "System",
                        SenderName = aiResponse.RequiresHumanAgent ? "Customer Service Agent" : "AI Assistant",
                        MessageType = "Text",
                        SentAt = DateTime.UtcNow.AddSeconds(3), // Simulate response delay
                        IsRead = false
                    };

                    _db.ChatMessages.Add(autoMessage);
                    await _db.SaveChangesAsync();

                    autoResponseDto = new ChatMessageDto
                    {
                        Id = autoMessage.Id,
                        ConversationId = autoMessage.ConversationId,
                        Content = autoMessage.Content,
                        SenderType = autoMessage.SenderType,
                        SenderName = autoMessage.SenderName ?? "",
                        SentAt = autoMessage.SentAt,
                        IsRead = autoMessage.IsRead,
                        MessageType = autoMessage.MessageType
                    };
                }

                await transaction.CommitAsync();

                var result = new ChatMessageResponseDto
                {
                    Message = userMessageDto,
                    HasAutoResponse = autoResponseDto != null,
                    AutoResponse = autoResponseDto
                };

                return ApiResponse<ChatMessageResponseDto>.Ok("Tin nhắn đã được gửi thành công.", result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return ApiResponse<ChatMessageResponseDto>.Fail($"Lỗi khi gửi tin nhắn: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> MarkMessagesAsReadAsync(int userId, int conversationId)
        {
            var conversation = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null)
                return ApiResponse<bool>.Fail("Không tìm thấy cuộc hội thoại.");

            await _db.ChatMessages
                .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderType != "Customer")
                .ExecuteUpdateAsync(m => m.SetProperty(p => p.IsRead, true));

            return ApiResponse<bool>.Ok("Đã đánh dấu tin nhắn là đã đọc.", true);
        }

        public async Task<ApiResponse<List<ChatMessageDto>>> GetNewMessagesAsync(int userId, int conversationId, DateTime since)
        {
            var conversation = await _db.ChatConversations
                .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

            if (conversation == null)
                return ApiResponse<List<ChatMessageDto>>.Fail("Không tìm thấy cuộc hội thoại.");

            var newMessages = await _db.ChatMessages
                .Where(m => m.ConversationId == conversationId && m.SentAt > since)
                .OrderBy(m => m.SentAt)
                .Select(m => new ChatMessageDto
                {
                    Id = m.Id,
                    ConversationId = m.ConversationId,
                    Content = m.Content,
                    SenderType = m.SenderType,
                    SenderName = m.SenderName ?? "",
                    SentAt = m.SentAt,
                    IsRead = m.IsRead,
                    AttachmentUrl = m.AttachmentUrl,
                    MessageType = m.MessageType
                })
                .ToListAsync();

            return ApiResponse<List<ChatMessageDto>>.Ok("Lấy tin nhắn mới thành công.", newMessages);
        }

        // ═══════════════════════════════════════════════════════════════
        //  CUSTOMER SERVICE BOT
        // ═══════════════════════════════════════════════════════════════

        public async Task<CustomerServiceResponse> GenerateAutoResponseAsync(string userMessage, int userId)
        {
            // Simulate processing delay
            await Task.Delay(100);

            var message = userMessage.ToLower().Trim();

            // Card-related inquiries
            if (ContainsAny(message, ["thẻ", "card", "tín dụng", "credit", "hạn mức", "limit"]))
            {
                if (ContainsAny(message, ["đăng ký", "làm thẻ", "apply", "mới"]))
                {
                    return new CustomerServiceResponse
                    {
                        Content = "Để đăng ký thẻ tín dụng MasterCredit, bạn có thể:\n\n1. Truy cập trang \"Duyệt thẻ\" trên ứng dụng\n2. Chọn loại thẻ phù hợp (Classic, Gold, Platinum)\n3. Điền thông tin và hoàn tất 7 bước đăng ký\n\nBạn cần những giấy tờ gì:\n• CCCD/CMND\n• Bảng lương gần nhất\n• Thu nhập tối thiểu: 10 triệu/tháng\n\nCó cần hỗ trợ thêm về quy trình đăng ký không?",
                        ResponseType = "Auto",
                        SuggestedActions = ["Xem các loại thẻ", "Bắt đầu đăng ký", "Kiểm tra điều kiện"]
                    };
                }

                if (ContainsAny(message, ["phí", "fee", "chi phí", "cost", "thường niên"]))
                {
                    return new CustomerServiceResponse
                    {
                        Content = "Thông tin phí thẻ tín dụng MasterCredit:\n\n🔹 **Classic Card**: MIỄN PHÍ thường niên\n🔹 **Gold Card**: 500,000 VND/năm\n🔹 **Platinum Card**: 2,000,000 VND/năm\n\n**Các phí khác:**\n• Rút tiền mặt: 3% (tối thiểu 50,000 VND)\n• Chuyển đổi ngoại tệ: 1.5%\n• Thanh toán trễ: 200,000 VND\n\nBạn muốn biết thêm thông tin về loại thẻ nào?",
                        ResponseType = "Auto",
                        SuggestedActions = ["So sánh thẻ", "Tính phí chi tiết", "Ưu đãi hiện tại"]
                    };
                }

                if (ContainsAny(message, ["hạn mức", "limit", "mức chi tiêu"]))
                {
                    return new CustomerServiceResponse
                    {
                        Content = "Hạn mức tín dụng MasterCredit:\n\n💳 **Classic**: 10 - 50 triệu VND\n💳 **Gold**: 50 - 200 triệu VND  \n💳 **Platinum**: 200 - 1 tỷ VND\n\n**Hạn mức được tính dựa trên:**\n• Thu nhập hàng tháng\n• Lịch sử tín dụng\n• Tài sản đảm bảo\n• Thời gian làm việc\n\nMuốn kiểm tra hạn mức dự kiến cho bạn không?",
                        ResponseType = "Auto",
                        SuggestedActions = ["Kiểm tra hạn mức", "Xem điều kiện tăng hạn mức", "Liên hệ tư vấn"]
                    };
                }
            }

            // Account-related inquiries
            if (ContainsAny(message, ["tài khoản", "account", "đăng nhập", "login", "mật khẩu", "password"]))
            {
                if (ContainsAny(message, ["quên mật khẩu", "forget password", "reset", "đổi mật khẩu"]))
                {
                    return new CustomerServiceResponse
                    {
                        Content = "Để khôi phục mật khẩu:\n\n1. Truy cập trang đăng nhập\n2. Nhấn \"Quên mật khẩu?\"\n3. Nhập email đã đăng ký\n4. Kiểm tra email và làm theo hướng dẫn\n5. Tạo mật khẩu mới\n\n⚠️ **Lưu ý bảo mật:**\n• Mật khẩu tối thiểu 8 ký tự\n• Bao gồm chữ hoa, chữ thường, số\n• Không chia sẻ mật khẩu với ai\n\nCần hỗ trợ thêm về bảo mật tài khoản?",
                        ResponseType = "Auto",
                        SuggestedActions = ["Hướng dẫn bảo mật", "Liên hệ hỗ trợ", "Cài đặt xác thực 2 bước"]
                    };
                }
            }

            // Transaction inquiries
            if (ContainsAny(message, ["giao dịch", "transaction", "lịch sử", "history", "chi tiêu"]))
            {
                return new CustomerServiceResponse
                {
                    Content = "Về tra cứu giao dịch:\n\n📊 **Xem lịch sử giao dịch:**\n• Truy cập mục \"Giao dịch\" trong ứng dụng\n• Lọc theo thời gian, loại giao dịch\n• Xuất báo cáo PDF/Excel\n\n🔍 **Tra cứu chi tiết:**\n• Thông tin merchant\n• Mã giao dịch\n• Thời gian thực hiện\n• Trạng thái giao dịch\n\nCó giao dịch nào bạn cần kiểm tra không?",
                    ResponseType = "Auto",
                    RequiresHumanAgent = true,
                    SuggestedActions = ["Báo cáo giao dịch lạ", "Xuất sao kê", "Liên hệ nhân viên"]
                };
            }

            // Complaint/Problem
            if (ContainsAny(message, ["khiếu nại", "complaint", "vấn đề", "problem", "lỗi", "error", "không hoạt động"]))
            {
                return new CustomerServiceResponse
                {
                    Content = "Tôi hiểu bạn đang gặp vấn đề. Để hỗ trợ tốt nhất:\n\n📝 **Vui lòng cung cấp:**\n• Mô tả chi tiết vấn đề\n• Thời gian xảy ra\n• Thiết bị đang sử dụng\n• Ảnh chụp màn hình (nếu có)\n\n⚡ **Tôi sẽ chuyển kết nối đến chuyên viên hỗ trợ** để giải quyết nhanh nhất cho bạn.\n\n🕒 Thời gian phản hồi: trong vòng 15 phút.",
                    ResponseType = "Escalated",
                    RequiresHumanAgent = true,
                    SuggestedActions = ["Gọi hotline", "Chat với chuyên viên", "Gửi email hỗ trợ"]
                };
            }

            // Promotion/Offers
            if (ContainsAny(message, ["ưu đãi", "promotion", "khuyến mãi", "offer", "giảm giá", "discount"]))
            {
                return new CustomerServiceResponse
                {
                    Content = "🎉 **Ưu đãi tháng này:**\n\n💳 **Thẻ mới:**\n• Miễn phí thường niên năm đầu (Gold/Platinum)\n• Cashback 5% cho 3 giao dịch đầu tiên\n• Quà tặng 500,000 VND khi chi tiêu 10 triệu\n\n🛍️ **Ưu đãi thương hiệu:**\n• Shopee: Giảm 15% tối đa 200K\n• Grab: Hoàn tiền 50% cho 10 chuyến\n• CGV: Mua 1 tặng 1 vé xem phim\n\n➡️ Bạn muốn biết chi tiết ưu đãi nào?",
                    ResponseType = "Auto",
                    SuggestedActions = ["Xem tất cả ưu đãi", "Đăng ký nhận thông báo", "So sánh thẻ"]
                };
            }

            // General/Greeting
            if (ContainsAny(message, ["xin chào", "hello", "hi", "chào", "hỗ trợ", "help", "giúp đỡ"]))
            {
                return new CustomerServiceResponse
                {
                    Content = "Xin chào! Tôi là AI Assistant của MasterCredit 👋\n\nTôi có thể hỗ trợ bạn:\n\n🏦 **Sản phẩm & Dịch vụ:**\n• Thông tin các loại thẻ tín dụng\n• Quy trình đăng ký thẻ\n• Phí và lãi suất\n\n💳 **Quản lý tài khoản:**\n• Tra cứu giao dịch\n• Hạn mức và thanh toán\n• Bảo mật tài khoản\n\n🎁 **Ưu đãi:**\n• Chương trình khuyến mãi\n• Cashback và điểm thưởng\n\nBạn cần hỗ trợ gì hôm nay? 😊",
                    ResponseType = "Auto",
                    SuggestedActions = ["Đăng ký thẻ mới", "Xem ưu đãi", "Liên hệ nhân viên"]
                };
            }

            // Default response
            return new CustomerServiceResponse
            {
                Content = "Cảm ơn bạn đã liên hệ! Tôi chưa hiểu rõ yêu cầu của bạn. \n\n🤖 **Một số chủ đề tôi có thể hỗ trợ:**\n• Thông tin thẻ tín dụng\n• Đăng ký thẻ mới\n• Tra cứu giao dịch\n• Ưu đãi và khuyến mãi\n• Quản lý tài khoản\n\n💬 Bạn có thể mô tả rõ hơn về vấn đề cần hỗ trợ không? Hoặc tôi có thể chuyển đến nhân viên tư vấn nếu cần.",
                ResponseType = "Auto",
                SuggestedActions = ["Nói chuyện với nhân viên", "Hướng dẫn sử dụng", "Câu hỏi thường gặp"]
            };
        }

        private static bool ContainsAny(string text, string[] keywords)
        {
            return keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }
    }
}