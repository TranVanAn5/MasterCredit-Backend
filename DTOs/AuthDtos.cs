using System.ComponentModel.DataAnnotations;

namespace backend.DTOs
{
    // ==================== REGISTER DTOs ====================

    /// <summary>Bước 1: Nhập thông tin cá nhân + mật khẩu</summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [MinLength(8, ErrorMessage = "Mật khẩu phải có ít nhất 8 ký tự")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Bước 3: Xác thực OTP (dùng cho cả Register và Login)</summary>
    public class VerifyOtpDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã OTP là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số")]
        public string OtpCode { get; set; } = string.Empty;
    }

    /// <summary>Bước 4: Upload ảnh 2 mặt CCCD</summary>
    public class UploadCitizenIdDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ảnh mặt trước CCCD là bắt buộc")]
        public IFormFile CitizenImgFront { get; set; } = null!;

        [Required(ErrorMessage = "Ảnh mặt sau CCCD là bắt buộc")]
        public IFormFile CitizenImgBack { get; set; } = null!;
    }

    /// <summary>Bước 5: Thiết lập mã PIN</summary>
    public class SetPinDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mã PIN là bắt buộc")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã PIN phải có 6 chữ số")]
        public string Pin { get; set; } = string.Empty;

        [Required(ErrorMessage = "Xác nhận mã PIN là bắt buộc")]
        [Compare("Pin", ErrorMessage = "Mã PIN xác nhận không khớp")]
        public string ConfirmPin { get; set; } = string.Empty;
    }

    // ==================== LOGIN DTOs ====================

    /// <summary>Bước 1+2: Đăng nhập bằng username + mật khẩu</summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Username là bắt buộc")]
        public string Username { get; set; } = string.Empty;  // Email hoặc số điện thoại

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        public string Password { get; set; } = string.Empty;
    }

    // ==================== RESEND OTP DTO ====================

    public class ResendOtpDto
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại OTP là bắt buộc")]
        public string OtpType { get; set; } = string.Empty;  // "Register" hoặc "Login"
    }

    // ==================== RESPONSE DTOs ====================

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        public static ApiResponse<T> Ok(string message, T? data = default) =>
            new() { Success = true, Message = message, Data = data };

        public static ApiResponse<T> Fail(string message) =>
            new() { Success = false, Message = message };
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }

    public class UserProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? CitizenImgFront { get; set; }
        public string? CitizenImgBack { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
