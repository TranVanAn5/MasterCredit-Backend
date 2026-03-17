using System.Security.Claims;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/chat")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // ════════════════════════════════════════════════════════════════
        //  CONVERSATION MANAGEMENT
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tạo cuộc hội thoại mới với customer service
        /// </summary>
        /// <remarks>
        /// Request body:
        ///
        ///     {
        ///       "subject": "Hỏi về thẻ tín dụng",
        ///       "initialMessage": "Tôi muốn đăng ký thẻ Gold"
        ///     }
        ///
        /// Response bao gồm thông tin conversation và tin nhắn chào mừng tự động.
        /// </remarks>
        [HttpPost("conversations")]
        public async Task<IActionResult> StartConversation([FromBody] StartChatConversationDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.StartConversationAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lấy danh sách tất cả cuộc hội thoại của người dùng
        /// </summary>
        /// <remarks>
        /// Response bao gồm:
        /// - Danh sách conversations (mới nhất trước)
        /// - Tổng số tin nhắn chưa đọc
        /// - Số conversations đang hoạt động
        /// </remarks>
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.GetUserConversationsAsync(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lấy chi tiết cuộc hội thoại với tất cả tin nhắn
        /// </summary>
        [HttpGet("conversations/{conversationId:int}")]
        public async Task<IActionResult> GetConversationDetail(int conversationId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.GetConversationDetailAsync(userId, conversationId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// Đóng cuộc hội thoại
        /// </summary>
        /// <remarks>
        /// Khi đóng conversation:
        /// - Status chuyển thành "Closed"
        /// - Thêm tin nhắn hệ thống thông báo đóng
        /// - Không thể gửi tin nhắn mới
        /// </remarks>
        [HttpPut("conversations/{conversationId:int}/close")]
        public async Task<IActionResult> CloseConversation(int conversationId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.CloseConversationAsync(userId, conversationId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════════════════════
        //  MESSAGE MANAGEMENT
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Gửi tin nhắn trong cuộc hội thoại
        /// </summary>
        /// <remarks>
        /// Request body:
        ///
        ///     {
        ///       "content": "Tôi muốn biết về phí thẻ năm",
        ///       "messageType": "Text"
        ///     }
        ///
        /// Response bao gồm:
        /// - Tin nhắn của người dùng vừa gửi
        /// - Tin nhắn phản hồi tự động từ AI assistant (nếu có)
        /// </remarks>
        [HttpPost("conversations/{conversationId:int}/messages")]
        public async Task<IActionResult> SendMessage(int conversationId, [FromBody] SendChatMessageDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.SendMessageAsync(userId, conversationId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Đánh dấu tất cả tin nhắn trong cuộc hội thoại là đã đọc
        /// </summary>
        [HttpPut("conversations/{conversationId:int}/mark-read")]
        public async Task<IActionResult> MarkMessagesAsRead(int conversationId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.MarkMessagesAsReadAsync(userId, conversationId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lấy tin nhắn mới từ thời điểm cụ thể (dùng cho real-time polling)
        /// </summary>
        /// <param name="conversationId">ID cuộc hội thoại</param>
        /// <param name="since">Timestamp ISO 8601 để lấy tin nhắn từ thời điểm đó</param>
        /// <remarks>
        /// Sử dụng để polling tin nhắn mới:
        ///
        ///     GET /api/chat/conversations/123/messages/new?since=2024-03-17T10:30:00Z
        ///
        /// Frontend có thể gọi API này mỗi 2-3 giây để cập nhật tin nhắn mới.
        /// </remarks>
        [HttpGet("conversations/{conversationId:int}/messages/new")]
        public async Task<IActionResult> GetNewMessages(int conversationId, [FromQuery] DateTime since)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _chatService.GetNewMessagesAsync(userId, conversationId, since);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════════════════════
        //  CUSTOMER SERVICE STATUS
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Lấy trạng thái hệ thống hỗ trợ khách hàng
        /// </summary>
        /// <remarks>
        /// Response:
        ///
        ///     {
        ///       "success": true,
        ///       "data": {
        ///         "isOnline": true,
        ///         "averageResponseTime": "2 phút",
        ///         "availableAgents": 5,
        ///         "queueCount": 3,
        ///         "serviceHours": "24/7",
        ///         "features": ["AI Assistant", "Human Agent", "Screen Share"]
        ///       }
        ///     }
        /// </remarks>
        [HttpGet("status")]
        public IActionResult GetServiceStatus()
        {
            var status = new
            {
                IsOnline = true,
                AverageResponseTime = "< 2 phút",
                AvailableAgents = 8,
                QueueCount = 2,
                ServiceHours = "24/7",
                Features = new[] { "AI Assistant", "Nhân viên hỗ trợ", "Hỗ trợ đa ngôn ngữ", "Chia sẻ tài liệu" },
                AiAssistant = new
                {
                    Name = "MasterCredit Assistant",
                    Version = "1.0",
                    Languages = new[] { "Tiếng Việt", "English" },
                    Capabilities = new[] { "Tư vấn sản phẩm", "Hướng dẫn sử dụng", "Tra cứu thông tin", "Xử lý khiếu nại cơ bản" }
                }
            };

            return Ok(ApiResponse<object>.Ok("Lấy trạng thái dịch vụ thành công.", status));
        }

        /// <summary>
        /// Lấy danh sách câu hỏi thường gặp (FAQ)
        /// </summary>
        [HttpGet("faq")]
        public IActionResult GetFrequentlyAskedQuestions()
        {
            var faqs = new[]
            {
                new {
                    Category = "Thẻ tín dụng",
                    Question = "Làm thế nào để đăng ký thẻ tín dụng?",
                    Answer = "Bạn có thể đăng ký online qua ứng dụng hoặc website chính thức với 7 bước đơn giản: chọn thẻ → nhập thông tin tài chính → upload giấy tờ → xem lại → xác nhận OTP.",
                    Keywords = new[] { "đăng ký", "thẻ mới", "apply" }
                },
                new {
                    Category = "Phí và lãi suất",
                    Question = "Phí thường niên của các loại thẻ là bao nhiêu?",
                    Answer = "Classic: MIỄN PHÍ, Gold: 500k/năm, Platinum: 2 triệu/năm. Thẻ mới được miễn phí năm đầu.",
                    Keywords = new[] { "phí", "thường niên", "cost" }
                },
                new {
                    Category = "Bảo mật",
                    Question = "Tôi quên mật khẩu, làm sao để lấy lại?",
                    Answer = "Nhấn 'Quên mật khẩu' ở trang đăng nhập → nhập email → làm theo hướng dẫn trong email để tạo mật khẩu mới.",
                    Keywords = new[] { "quên mật khẩu", "reset", "bảo mật" }
                },
                new {
                    Category = "Giao dịch",
                    Question = "Làm sao để kiểm tra lịch sử giao dịch?",
                    Answer = "Vào mục 'Giao dịch' trong ứng dụng → có thể lọc theo thời gian và loại giao dịch → xuất báo cáo nếu cần.",
                    Keywords = new[] { "lịch sử", "giao dịch", "sao kê" }
                },
                new {
                    Category = "Hạn mức",
                    Question = "Hạn mức tín dụng được tính như thế nào?",
                    Answer = "Dựa trên thu nhập, lịch sử tín dụng và tài sản. Classic: 10-50 triệu, Gold: 50-200 triệu, Platinum: 200 triệu - 1 tỷ.",
                    Keywords = new[] { "hạn mức", "limit", "tín dụng" }
                }
            };

            return Ok(ApiResponse<object>.Ok("Lấy FAQ thành công.", faqs));
        }

        // ════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return raw != null && int.TryParse(raw, out userId);
        }
    }
}