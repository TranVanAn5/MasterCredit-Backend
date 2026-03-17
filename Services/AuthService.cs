using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using BCrypt.Net;

namespace backend.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<AuthService> _logger;
        private static readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB

        public AuthService(AppDbContext db, IEmailService emailService,
            IConfiguration config, IWebHostEnvironment env, ILogger<AuthService> logger)
        {
            _db = db;
            _emailService = emailService;
            _config = config;
            _env = env;
            _logger = logger;
        }

        // =========================================================
        // REGISTER - Bước 1: Nhập thông tin + gửi OTP
        // =========================================================
        public async Task<ApiResponse<object>> RegisterAsync(RegisterDto dto)
        {
            try
            {
                if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                    return ApiResponse<object>.Fail("Email đã được sử dụng.");

                if (await _db.Users.AnyAsync(u => u.PhoneNumber == dto.PhoneNumber))
                    return ApiResponse<object>.Fail("Số điện thoại đã được sử dụng.");

                var user = new User
                {
                    Name = dto.Name,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    RoleId = 1,
                    IsEmailVerified = false,
                    IsActive = false,
                    // IsEmailVerified = true, // 👉 TESTING PURPOSES ONLY
                    // IsActive = true, // 👉 TESTING PURPOSES ONLY
                    CreatedAt = DateTime.UtcNow
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                await SendOtpAsync(user, "Register");

                return ApiResponse<object>.Ok($"OTP demo: 123456", new { Email = user.Email });
            }
            catch (InvalidOperationException ex)
            {
                // This is from SendOtpAsync failure
                return ApiResponse<object>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for email {Email}", dto.Email);
                return ApiResponse<object>.Fail("Đã có lỗi xảy ra trong quá trình đăng ký. Vui lòng thử lại.");
            }
        }

        // =========================================================
        // REGISTER - Bước 3: Xác thực OTP
        // =========================================================
        public async Task<ApiResponse<object>> VerifyRegisterOtpAsync(VerifyOtpDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return ApiResponse<object>.Fail("Email không tồn tại.");

            if (user.IsEmailVerified)
            {
                return ApiResponse<object>.Ok("Email đã xác thực rồi, tiếp tục bước sau.");
            }

            var valid = await ValidateOtpAsync(user.Id, dto.OtpCode, "Register");
            if (!valid)
                return ApiResponse<object>.Fail("Mã OTP không hợp lệ hoặc đã hết hạn.");

            user.IsEmailVerified = true;
            await _db.SaveChangesAsync();

            return ApiResponse<object>.Ok("Xác thực email thành công. Vui lòng tải lên ảnh CCCD.");
        }

        // =========================================================
        // REGISTER - Bước 4: Upload ảnh CCCD
        // =========================================================
        public async Task<ApiResponse<object>> UploadCitizenIdAsync(UploadCitizenIdDto dto)
        {
            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                    return ApiResponse<object>.Fail("Email không tồn tại.");

                if (!user.IsEmailVerified)
                    return ApiResponse<object>.Fail("Vui lòng xác thực email trước.");

                // Validate file size and type
                var frontValidation = ValidateFile(dto.CitizenImgFront, "Ảnh mặt trước CCCD");
                if (!frontValidation.isValid)
                    return ApiResponse<object>.Fail(frontValidation.errorMessage);

                var backValidation = ValidateFile(dto.CitizenImgBack, "Ảnh mặt sau CCCD");
                if (!backValidation.isValid)
                    return ApiResponse<object>.Fail(backValidation.errorMessage);

                var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "citizen-id");
                Directory.CreateDirectory(uploadDir);

                var frontExt = ValidateAndGetExtension(dto.CitizenImgFront.FileName);
                var backExt = ValidateAndGetExtension(dto.CitizenImgBack.FileName);

                var frontFileName = $"{user.Id}_front_{Guid.NewGuid()}{frontExt}";
                var backFileName = $"{user.Id}_back_{Guid.NewGuid()}{backExt}";

                var frontPath = Path.Combine(uploadDir, frontFileName);
                var backPath = Path.Combine(uploadDir, backFileName);

                // Save files with proper disposal
                await using (var frontStream = new FileStream(frontPath, FileMode.Create))
                {
                    await dto.CitizenImgFront.CopyToAsync(frontStream);
                }

                await using (var backStream = new FileStream(backPath, FileMode.Create))
                {
                    await dto.CitizenImgBack.CopyToAsync(backStream);
                }

                user.CitizenImgFront = $"/uploads/citizen-id/{frontFileName}";
                user.CitizenImgBack = $"/uploads/citizen-id/{backFileName}";
                await _db.SaveChangesAsync();

                return ApiResponse<object>.Ok("Tải lên ảnh CCCD thành công. Vui lòng thiết lập mã PIN.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading citizen ID for user {Email}", dto.Email);
                return ApiResponse<object>.Fail("Đã có lỗi xảy ra khi tải ảnh lên. Vui lòng thử lại.");
            }
        }

        // =========================================================
        // REGISTER - Bước 5: Thiết lập mã PIN
        // =========================================================
        public async Task<ApiResponse<object>> SetPinAsync(SetPinDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return ApiResponse<object>.Fail("Email không tồn tại.");

            if (!user.IsEmailVerified)
                return ApiResponse<object>.Fail("Vui lòng xác thực email trước.");

            if (user.CitizenImgFront == null || user.CitizenImgBack == null)
                return ApiResponse<object>.Fail("Vui lòng tải lên ảnh CCCD trước.");

            user.PinHash = BCrypt.Net.BCrypt.HashPassword(dto.Pin);
            user.IsActive = true;
            await _db.SaveChangesAsync();

            return ApiResponse<object>.Ok("Đăng ký tài khoản thành công! Vui lòng đăng nhập.");
        }

        // =========================================================
        // LOGIN - Bước 1+2: Kiểm tra username + password → gửi OTP
        // =========================================================
        public async Task<ApiResponse<object>> LoginAsync(LoginDto dto)
        {
            try
            {
                var user = await _db.Users.Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.Email == dto.Username || u.PhoneNumber == dto.Username);

                if (user == null)
                    return ApiResponse<object>.Fail("Tài khoản không tồn tại.");

                if (!user.IsActive)
                    return ApiResponse<object>.Fail("Tài khoản chưa được kích hoạt. Vui lòng hoàn tất đăng ký.");

                if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                    return ApiResponse<object>.Fail("Mật khẩu không chính xác.");

                await SendOtpAsync(user, "Login");

                // return ApiResponse<object>.Ok($"Mã OTP đã được gửi đến email {MaskEmail(user.Email)}. Vui lòng kiểm tra hộp thư.", new { Email = user.Email });
                return ApiResponse<object>.Ok($"OTP demo: 123456", new { Email = user.Email });
            }
            catch (InvalidOperationException ex)
            {
                // This is from SendOtpAsync failure
                return ApiResponse<object>.Fail(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for username {Username}", dto.Username);
                return ApiResponse<object>.Fail("Đã có lỗi xảy ra trong quá trình đăng nhập. Vui lòng thử lại.");
            }
        }

        // =========================================================
        // LOGIN - Bước 4: Xác thực OTP → trả về JWT
        // =========================================================
        public async Task<ApiResponse<LoginResponseDto>> VerifyLoginOtpAsync(VerifyOtpDto dto)
        {
            var user = await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null)
                return ApiResponse<LoginResponseDto>.Fail("Email không tồn tại.");

            if (!user.IsActive)
                return ApiResponse<LoginResponseDto>.Fail("Tài khoản chưa được kích hoạt.");

            var valid = await ValidateOtpAsync(user.Id, dto.OtpCode, "Login");
            if (!valid)
                return ApiResponse<LoginResponseDto>.Fail("Mã OTP không hợp lệ hoặc đã hết hạn.");

            var token = GenerateJwtToken(user);

            return ApiResponse<LoginResponseDto>.Ok("Đăng nhập thành công!", new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role?.Name ?? "User"
            });
        }

        // =========================================================
        // RESEND OTP
        // =========================================================
        public async Task<ApiResponse<object>> ResendOtpAsync(ResendOtpDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null)
                return ApiResponse<object>.Fail("Email không tồn tại.");

            // Fixed logic: Check if last OTP was created less than 1 minute ago
            var lastOtp = await _db.OTPs
                .Where(o => o.UserId == user.Id && o.OTPType == dto.OtpType)
                .OrderByDescending(o => o.Id)
                .FirstOrDefaultAsync();

            if (lastOtp != null)
            {
                // Assuming OTP table has CreatedAt field, if not, we use ExpiryTime - 5 minutes
                var otpCreatedAt = lastOtp.ExpiryTime.AddMinutes(-5); // Since OTP expires in 5 minutes
                if (otpCreatedAt > DateTime.UtcNow.AddMinutes(-1))
                    return ApiResponse<object>.Fail("Vui lòng chờ ít nhất 1 phút trước khi gửi lại OTP.");
            }

            try
            {
                await SendOtpAsync(user, dto.OtpType);
                return ApiResponse<object>.Ok("OTP demo: 123456");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP resend for user {Email}", dto.Email);
                return ApiResponse<object>.Fail("Không thể gửi OTP. Vui lòng thử lại sau.");
            }
        }

        // =========================================================
        // PRIVATE HELPERS
        // =========================================================

        // private async Task SendOtpAsync(User user, string otpType)
        // {
        //     try
        //     {
        //         // Use transaction to ensure atomicity and prevent race conditions
        //         using var transaction = await _db.Database.BeginTransactionAsync();

        //         try
        //         {
        //             // Remove old unused OTPs of the same type
        //             var oldOtps = await _db.OTPs
        //                 .Where(o => o.UserId == user.Id && o.OTPType == otpType && !o.IsUsed)
        //                 .ToListAsync();
        //             _db.OTPs.RemoveRange(oldOtps);

        //             // Generate secure random OTP
        //             // var otpCode = GenerateSecureOtpCode();
        //             var otpCode = "123456";
        //             var newOtp = new OTP
        //             {
        //                 OTPCode = otpCode,
        //                 OTPType = otpType,
        //                 ExpiryTime = DateTime.UtcNow.AddMinutes(5),
        //                 IsUsed = false,
        //                 UserId = user.Id
        //             };

        //             _db.OTPs.Add(newOtp);
        //             await _db.SaveChangesAsync();

        //             // Try to send email - if this fails, rollback the transaction
        //             // await _emailService.SendOtpEmailAsync(user.Email, user.Name, otpCode, otpType);
        //             return ApiResponse<object>.Ok("OTP test: 123456");

        //             await transaction.CommitAsync();
        //         }
        //         catch
        //         {
        //             await transaction.RollbackAsync();
        //             throw;
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Failed to send OTP for user {UserId}, type {OtpType}", user.Id, otpType);
        //         throw new InvalidOperationException("Không thể gửi mã OTP. Vui lòng thử lại.");
        //     }
        // }
        private async Task SendOtpAsync(User user, string otpType)
        {
            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();

                try
                {
                    // Xóa OTP cũ
                    var oldOtps = await _db.OTPs
                        .Where(o => o.UserId == user.Id && o.OTPType == otpType && !o.IsUsed)
                        .ToListAsync();

                    _db.OTPs.RemoveRange(oldOtps);

                    // 👉 HARD CODE OTP
                    var otpCode = "123456";

                    var newOtp = new OTP
                    {
                        OTPCode = otpCode,
                        OTPType = otpType,
                        ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                        IsUsed = false,
                        UserId = user.Id
                    };

                    _db.OTPs.Add(newOtp);
                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync(); // ✅ QUAN TRỌNG
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP");
                throw new InvalidOperationException("Không thể tạo OTP.");
            }
        }

        private async Task<bool> ValidateOtpAsync(int userId, string otpCode, string otpType)
        {
            var otp = await _db.OTPs.FirstOrDefaultAsync(o =>
                o.UserId == userId &&
                o.OTPCode == otpCode &&
                o.OTPType == otpType &&
                !o.IsUsed &&
                o.ExpiryTime > DateTime.UtcNow);

            if (otp == null) return false;

            otp.IsUsed = true;
            await _db.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            // Validate configuration
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];
            var expireMinutesStr = _config["Jwt:ExpireMinutes"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) ||
                string.IsNullOrEmpty(jwtAudience) || string.IsNullOrEmpty(expireMinutesStr))
            {
                _logger.LogError("JWT configuration is missing or incomplete");
                throw new InvalidOperationException("JWT configuration is not properly configured");
            }

            if (!int.TryParse(expireMinutesStr, out var expireMinutes))
            {
                _logger.LogWarning("Invalid JWT ExpireMinutes configuration, using default 1440");
                expireMinutes = 1440;
            }

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier,     user.Id.ToString()),
                new Claim(ClaimTypes.Name,               user.Name),
                new Claim(ClaimTypes.Role,               user.Role?.Name ?? "User")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 2) return email;
            return email[..2] + new string('*', at - 2) + email[at..];
        }

        private static string GenerateSecureOtpCode()
        {
            // Use cryptographically secure random number generator
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            return (100000 + (value % 900000)).ToString();
        }

        private static (bool isValid, string errorMessage) ValidateFile(IFormFile file, string fieldName)
        {
            if (file == null)
                return (false, $"{fieldName} là bắt buộc.");

            if (file.Length == 0)
                return (false, $"{fieldName} không được để trống.");

            if (file.Length > MaxFileSizeBytes)
                return (false, $"{fieldName} không được vượt quá 5MB.");

            var extension = Path.GetExtension(file.FileName?.ToLowerInvariant() ?? "");
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
                return (false, $"{fieldName} chỉ được phép có định dạng: {string.Join(", ", _allowedExtensions)}.");

            // Basic MIME type validation
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType?.ToLowerInvariant()))
                return (false, $"{fieldName} có định dạng file không hợp lệ.");

            return (true, "");
        }

        private static string ValidateAndGetExtension(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                throw new ArgumentException("File name cannot be null or empty");

            var extension = Path.GetExtension(fileName.ToLowerInvariant());
            if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
                throw new ArgumentException($"Invalid file extension: {extension}");

            return extension;
        }
    }
}
