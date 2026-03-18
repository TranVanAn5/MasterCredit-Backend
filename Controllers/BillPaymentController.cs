using System.Security.Claims;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/bill-payment")]
    public class BillPaymentController : ControllerBase
    {
        private readonly IBillPaymentService _billPaymentService;
        private readonly ICardService _cardService;

        public BillPaymentController(
            IBillPaymentService billPaymentService,
            ICardService cardService)
        {
            _billPaymentService = billPaymentService;
            _cardService = cardService;
        }

        // ════════════════════════════════════════════════════════════════
        //  BILL PAYMENT – 6-STEP FLOW
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// B1: Lấy danh sách loại hóa đơn (điện, nước, wifi, học phí...).
        /// </summary>
        /// <remarks>
        /// Public endpoint - không cần authentication.
        /// Response trả về danh sách categories với icon và display order.
        /// </remarks>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
            => Ok(await _billPaymentService.GetBillCategoriesAsync());

        /// <summary>
        /// B2: Lấy danh sách nhà cung cấp theo category đã chọn.
        /// </summary>
        /// <remarks>
        /// Public endpoint - không cần authentication.
        /// Response trả về danh sách providers với thông tin service fee và logo.
        /// </remarks>
        [HttpGet("categories/{categoryId:int}/providers")]
        public async Task<IActionResult> GetProvidersByCategory(int categoryId)
            => Ok(await _billPaymentService.GetProvidersByCategoryAsync(categoryId));

        /// <summary>
        /// B3: Xác thực mã khách hàng và lấy thông tin hóa đơn.
        /// </summary>
        /// <remarks>
        /// Request body:
        ///
        ///     {
        ///       "providerId": 1,
        ///       "customerCode": "KH123456789"
        ///     }
        ///
        /// Response trả về thông tin hóa đơn (số tiền, tên khách hàng, địa chỉ, phí dịch vụ).
        /// </remarks>
        [HttpPost("verify-customer")]
        public async Task<IActionResult> VerifyCustomer([FromBody] VerifyCustomerRequest request)
        {
            var result = await _billPaymentService.VerifyCustomerAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// B4: Lấy danh sách thẻ của người dùng để chọn thẻ thanh toán.
        /// </summary>
        /// <remarks>
        /// Requires authentication.
        /// Endpoint này sử dụng CardService.GetUserCardsAsync() đã có sẵn.
        /// Hoặc người dùng có thể gọi trực tiếp GET /api/cards.
        /// </remarks>
        [HttpGet("cards")]
        [Authorize]
        public async Task<IActionResult> GetCards()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _cardService.GetUserCardsAsync(userId));
        }

        /// <summary>
        /// B5+B6: Xử lý thanh toán hóa đơn với mã PIN.
        /// </summary>
        /// <remarks>
        /// Requires authentication.
        /// Request body:
        ///
        ///     {
        ///       "providerId": 1,
        ///       "customerCode": "KH123456789",
        ///       "cardId": 2,
        ///       "pin": "123456",
        ///       "billAmount": 250000,
        ///       "serviceFee": 2000,
        ///       "totalAmount": 252000,
        ///       "customerName": "Nguyễn Văn An",
        ///       "customerAddress": "12 Trần Hưng Đạo, P.1, Q.5, TP.HCM"
        ///     }
        ///
        /// Response:
        /// - Success: Trả về thông tin thanh toán với reference number
        /// - Fail: Trả về lỗi (PIN sai, thẻ không đủ hạn mức, etc.)
        /// </remarks>
        [HttpPost("process")]
        [Authorize]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _billPaymentService.ProcessPaymentAsync(userId, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Lấy lịch sử thanh toán hóa đơn của người dùng.
        /// </summary>
        /// <remarks>
        /// Requires authentication.
        /// Response trả về danh sách tất cả các giao dịch thanh toán hóa đơn.
        /// </remarks>
        [HttpGet("history")]
        [Authorize]
        public async Task<IActionResult> GetPaymentHistory()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _billPaymentService.GetPaymentHistoryAsync(userId));
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPERS
        // ════════════════════════════════════════════════════════════════

        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out userId);
        }
    }
}
