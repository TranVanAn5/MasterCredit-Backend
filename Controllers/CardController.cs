using System.Security.Claims;
using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/cards")]
    [Authorize]
    public class CardController : ControllerBase
    {
        private readonly ICardService _cardService;

        public CardController(ICardService cardService)
        {
            _cardService = cardService;
        }

        // ════════════════════════════════════════════════════════════════
        //  CATALOG (public)
        // ════════════════════════════════════════════════════════════════

        /// <summary>B1: Lấy tất cả loại thẻ hiện có để người dùng chọn.</summary>
        [HttpGet("types")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCardTypes()
            => Ok(await _cardService.GetAllCardTypesAsync());

        /// <summary>Lấy chi tiết một loại thẻ.</summary>
        [HttpGet("types/{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCardTypeById(int id)
            => Ok(await _cardService.GetCardTypeByIdAsync(id));

        /// <summary>So sánh 2–3 loại thẻ.</summary>
        [HttpPost("compare")]
        [AllowAnonymous]
        public async Task<IActionResult> CompareCardTypes([FromBody] List<int> cardTypeIds)
            => Ok(await _cardService.CompareCardTypesAsync(cardTypeIds));

        // ════════════════════════════════════════════════════════════════
        //  MY CARDS (requires auth)
        // ════════════════════════════════════════════════════════════════

        /// <summary>Lấy danh sách thẻ đã được cấp của người dùng đang đăng nhập.</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyCards()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _cardService.GetUserCardsAsync(userId));
        }

        /// <summary>Lấy chi tiết thông tin của một thẻ cụ thể (bao gồm CVV và số thẻ đầy đủ).</summary>
        [HttpGet("{cardId:int}")]
        public async Task<IActionResult> GetCardDetail(int cardId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardService.GetCardDetailAsync(userId, cardId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ════════════════════════════════════════════════════════════════
        //  CARD APPLICATION – 7-STEP FLOW (requires auth)
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// B2: Gửi thông tin tài chính + loại thẻ mong muốn → tạo đơn nháp.
        /// </summary>
        /// <remarks>
        /// Request body:
        ///
        ///     {
        ///       "cardTypeId": 2,
        ///       "grossAnnualIncome": 240000000,
        ///       "incomeSource": "Lương",
        ///       "occupation": "Kỹ sư phần mềm",
        ///       "companyName": "Công ty ABC"
        ///     }
        ///
        /// Response trả về `applicationId` dùng cho các bước tiếp theo.
        /// </remarks>
        [HttpPost("apply/start")]
        public async Task<IActionResult> StartApplication([FromBody] StartApplicationDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardService.StartApplicationAsync(userId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// B3: Upload CCCD (mặt trước) và bảng lương cho đơn nháp.
        /// </summary>
        /// <remarks>
        /// Gửi dưới dạng `multipart/form-data` với 2 trường:
        /// - `idCard`    – ảnh CCCD mặt trước (.jpg/.png/.webp, tối đa 5 MB)
        /// - `salarySlip` – bảng lương (.jpg/.png/.pdf, tối đa 5 MB)
        /// </remarks>
        [HttpPost("apply/{applicationId:int}/upload-documents")]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20 MB tổng
        public async Task<IActionResult> UploadDocuments(
            int applicationId,
            IFormFile idCard,
            IFormFile salarySlip)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardService.UploadDocumentsAsync(userId, applicationId, idCard, salarySlip);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// B4: Xem lại toàn bộ thông tin đơn trước khi nộp.
        /// </summary>
        [HttpGet("apply/{applicationId:int}/review")]
        public async Task<IActionResult> ReviewApplication(int applicationId)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardService.GetApplicationReviewAsync(userId, applicationId);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>
        /// B5: Gửi mã OTP về email hoặc số điện thoại đã đăng ký.
        /// </summary>
        /// <remarks>
        /// Request body:
        ///
        ///     { "type": "email" }
        ///     // hoặc
        ///     { "type": "phone" }
        /// </remarks>
        [HttpPost("apply/{applicationId:int}/send-otp")]
        public async Task<IActionResult> SendOtp(
            int applicationId,
            [FromBody] SendApplicationOtpDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardService.SendApplicationOtpAsync(userId, applicationId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// B6: Nhập mã OTP để xác nhận và hoàn tất đơn đăng ký.
        /// B7 được thể hiện qua response thành công – đơn chuyển sang trạng thái Pending.
        /// </summary>
        /// <remarks>
        /// Request body:
        ///
        ///     { "otpCode": "123456" }
        /// </remarks>
        [HttpPost("apply/{applicationId:int}/submit")]
        public async Task<IActionResult> SubmitApplication(
            int applicationId,
            [FromBody] SubmitApplicationDto dto)
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            var result = await _cardService.FinalizeApplicationAsync(userId, applicationId, dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Lấy danh sách tất cả đơn đăng ký đã gửi (không bao gồm đơn nháp).</summary>
        [HttpGet("apply/my-applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            if (!TryGetUserId(out var userId)) return Unauthorized();
            return Ok(await _cardService.GetUserApplicationsAsync(userId));
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
