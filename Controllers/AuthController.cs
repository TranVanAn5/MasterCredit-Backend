using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using backend.DTOs;
using backend.Interfaces;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        // =====================================================
        // REGISTER FLOW
        // =====================================================

        /// <summary>
        /// Bước 1: Nhập thông tin cá nhân + mật khẩu → OTP được gửi tự động về Email.
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage))
                });

            var result = await _authService.RegisterAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Bước 3: Nhập mã OTP để xác thực email.
        /// </summary>
        [HttpPost("register/verify-otp")]
        public async Task<IActionResult> VerifyRegisterOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.VerifyRegisterOtpAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Bước 4: Upload 2 mặt Căn cước công dân (multipart/form-data).
        /// </summary>
        [HttpPost("register/upload-citizen-id")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadCitizenId([FromForm] UploadCitizenIdDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Kiểm tra định dạng file ảnh
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var frontExt = Path.GetExtension(dto.CitizenImgFront.FileName).ToLower();
            var backExt  = Path.GetExtension(dto.CitizenImgBack.FileName).ToLower();

            if (!allowedExtensions.Contains(frontExt) || !allowedExtensions.Contains(backExt))
                return BadRequest(ApiResponse<object>.Fail("Chỉ chấp nhận file ảnh JPG, PNG hoặc WEBP."));

            // Giới hạn kích thước 5MB
            if (dto.CitizenImgFront.Length > 5 * 1024 * 1024 || dto.CitizenImgBack.Length > 5 * 1024 * 1024)
                return BadRequest(ApiResponse<object>.Fail("Kích thước file không được vượt quá 5MB."));

            var result = await _authService.UploadCitizenIdAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Bước 5: Thiết lập mã PIN 6 chữ số. Hoàn tất đăng ký.
        /// </summary>
        [HttpPost("register/set-pin")]
        public async Task<IActionResult> SetPin([FromBody] SetPinDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.SetPinAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =====================================================
        // LOGIN FLOW
        // =====================================================

        /// <summary>
        /// Bước 1+2: Nhập username (Email hoặc SĐT) + mật khẩu → OTP được gửi về Email.
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>
        /// Bước 4: Nhập mã OTP → trả về JWT Token để truy cập Dashboard.
        /// </summary>
        [HttpPost("login/verify-otp")]
        public async Task<IActionResult> VerifyLoginOtp([FromBody] VerifyOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.VerifyLoginOtpAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =====================================================
        // SHARED
        // =====================================================

        /// <summary>
        /// Gửi lại mã OTP (cooldown 1 phút). OtpType: "Register" hoặc "Login".
        /// </summary>
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResendOtpAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // =====================================================
        // PROFILE
        // =====================================================

        /// <summary>
        /// Lấy thông tin profile của user hiện tại (yêu cầu JWT token).
        /// </summary>
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Lấy userId từ JWT token claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Token không hợp lệ."
                });

            var result = await _authService.GetProfileAsync(userId);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
