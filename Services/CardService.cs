using System.Security.Cryptography;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class CardService : ICardService
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _emailService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CardService> _logger;

        private static readonly string[] _allowedImageExts = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] _allowedDocExts = { ".jpg", ".jpeg", ".png", ".webp", ".pdf" };
        private static readonly string[] _allowedImageMime = { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        private static readonly string[] _allowedDocMime = { "image/jpeg", "image/jpg", "image/png", "image/webp", "application/pdf" };
        private const int MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB
        private const string OtpType = "CardApply";

        public CardService(
            AppDbContext db,
            IEmailService emailService,
            IWebHostEnvironment env,
            ILogger<CardService> logger)
        {
            _db = db;
            _emailService = emailService;
            _env = env;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //  CATALOG
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<List<CardDto>>> GetUserCardsAsync(int userId)
        {
            var cards = await _db.Cards
                .Include(c => c.CardType)
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            var result = cards.Select(c => new CardDto
            {
                Id = c.Id,
                CardNumber = MaskCardNumber(c.CardNumber),
                ExpiryDate = $"{c.ExpiryDate.Month:D2}/{c.ExpiryDate.Year % 100:D2}",
                CardStatus = c.CardStatus,
                HolderName = c.User.Name,
                CardType = MapCardType(c.CardType)
            }).ToList();

            return ApiResponse<List<CardDto>>.Ok("Lấy danh sách thẻ thành công.", result);
        }

        public async Task<ApiResponse<CardDetailDto>> GetCardDetailAsync(int userId, int cardId)
        {
            var card = await _db.Cards
                .Include(c => c.CardType)
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == cardId);

            if (card == null)
                return ApiResponse<CardDetailDto>.Fail("Không tìm thấy thẻ.");

            // Check if the card belongs to the user
            if (card.UserId != userId)
                return ApiResponse<CardDetailDto>.Fail("Bạn không có quyền xem thông tin thẻ này.");

            var result = new CardDetailDto
            {
                Id = card.Id,
                CardNumber = card.CardNumber, // Full card number
                CVV = card.CVV,
                ExpiryDate = $"{card.ExpiryDate.Month:D2}/{card.ExpiryDate.Year % 100:D2}",
                CardStatus = card.CardStatus,
                HolderName = card.User.Name,
                CardType = MapCardType(card.CardType),
                IssuedDate = DateTime.UtcNow.AddMonths(-6) // Mock issued date - replace with actual field if available
            };

            return ApiResponse<CardDetailDto>.Ok("Lấy chi tiết thẻ thành công.", result);
        }

        public async Task<ApiResponse<List<CardTypeInfoDto>>> GetAllCardTypesAsync()
        {
            var types = await _db.CardTypes
                .OrderBy(ct => ct.CreditLimit)
                .ToListAsync();

            return ApiResponse<List<CardTypeInfoDto>>.Ok(
                "Lấy danh sách loại thẻ thành công.",
                types.Select(ct => MapCardType(ct)).ToList());
        }

        public async Task<ApiResponse<List<CardTypeInfoDto>>> CompareCardTypesAsync(List<int> cardTypeIds)
        {
            if (cardTypeIds == null || cardTypeIds.Count < 2)
                return ApiResponse<List<CardTypeInfoDto>>.Fail("Vui lòng chọn ít nhất 2 thẻ để so sánh.");

            if (cardTypeIds.Count > 3)
                return ApiResponse<List<CardTypeInfoDto>>.Fail("Chỉ có thể so sánh tối đa 3 thẻ.");

            var types = await _db.CardTypes
                .Where(ct => cardTypeIds.Contains(ct.Id))
                .ToListAsync();

            if (types.Count != cardTypeIds.Count)
                return ApiResponse<List<CardTypeInfoDto>>.Fail("Một số loại thẻ không tồn tại.");

            var ordered = cardTypeIds
                .Select(id => types.FirstOrDefault(t => t.Id == id))
                .Where(t => t != null)
                .Select(t => MapCardType(t!))
                .ToList();

            return ApiResponse<List<CardTypeInfoDto>>.Ok("Lấy thông tin so sánh thẻ thành công.", ordered);
        }

        public async Task<ApiResponse<CardTypeInfoDto>> GetCardTypeByIdAsync(int cardTypeId)
        {
            var ct = await _db.CardTypes.FindAsync(cardTypeId);
            if (ct == null)
                return ApiResponse<CardTypeInfoDto>.Fail("Không tìm thấy loại thẻ.");

            return ApiResponse<CardTypeInfoDto>.Ok("Lấy thông tin thẻ thành công.", MapCardType(ct));
        }

        // ════════════════════════════════════════════════════════════════
        //  B2 – Tạo đơn nháp (Draft) với thông tin tài chính + loại thẻ
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<ApplicationReviewDto>> StartApplicationAsync(
            int userId, StartApplicationDto dto)
        {
            // Validate card type
            var cardType = await _db.CardTypes.FindAsync(dto.CardTypeId);
            if (cardType == null)
                return ApiResponse<ApplicationReviewDto>.Fail("Loại thẻ không tồn tại.");

            // Validate income
            if (dto.GrossAnnualIncome <= 0)
                return ApiResponse<ApplicationReviewDto>.Fail("Thu nhập hàng năm phải lớn hơn 0.");

            if (string.IsNullOrWhiteSpace(dto.IncomeSource))
                return ApiResponse<ApplicationReviewDto>.Fail("Vui lòng nhập nguồn thu nhập.");

            if (string.IsNullOrWhiteSpace(dto.Occupation))
                return ApiResponse<ApplicationReviewDto>.Fail("Vui lòng nhập nghề nghiệp.");

            if (string.IsNullOrWhiteSpace(dto.CompanyName))
                return ApiResponse<ApplicationReviewDto>.Fail("Vui lòng nhập tên công ty / nơi làm việc.");

            // Create draft application
            var application = new CardApplication
            {
                UserId = userId,
                CardTypeId = dto.CardTypeId,
                GrossAnnualIncome = dto.GrossAnnualIncome,
                IncomeSource = dto.IncomeSource.Trim(),
                Occupation = dto.Occupation.Trim(),
                CompanyName = dto.CompanyName.Trim(),
                IdCardPath = string.Empty,
                SalarySlipPath = string.Empty,
                Status = "Draft",
                ApplicationDate = DateTime.UtcNow
            };

            _db.CardApplications.Add(application);
            await _db.SaveChangesAsync();

            return ApiResponse<ApplicationReviewDto>.Ok(
                "Đã lưu thông tin tài chính. Vui lòng tải lên giấy tờ ở bước tiếp theo.",
                BuildReviewDto(application, cardType));
        }

        // ════════════════════════════════════════════════════════════════
        //  B3 – Upload CCCD và bảng lương
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<ApplicationReviewDto>> UploadDocumentsAsync(
            int userId, int applicationId, IFormFile idCard, IFormFile salarySlip)
        {
            var app = await _db.CardApplications
                .Include(a => a.CardType)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

            if (app == null)
                return ApiResponse<ApplicationReviewDto>.Fail("Không tìm thấy đơn đăng ký.");

            if (app.Status != "Draft")
                return ApiResponse<ApplicationReviewDto>.Fail("Đơn đăng ký đã được nộp, không thể thay đổi giấy tờ.");

            // Validate files
            var idVal = ValidateFile(idCard, "Ảnh CCCD", _allowedImageExts, _allowedImageMime);
            if (!idVal.ok) return ApiResponse<ApplicationReviewDto>.Fail(idVal.error);

            var slipVal = ValidateFile(salarySlip, "Bảng lương", _allowedDocExts, _allowedDocMime);
            if (!slipVal.ok) return ApiResponse<ApplicationReviewDto>.Fail(slipVal.error);

            // Delete old files if they exist
            DeleteFileIfExists(app.IdCardPath);
            DeleteFileIfExists(app.SalarySlipPath);

            // Save new files
            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "card-applications", userId.ToString());
            Directory.CreateDirectory(uploadDir);

            app.IdCardPath = await SaveFileAsync(idCard, uploadDir, $"{userId}_{applicationId}_idcard");
            app.SalarySlipPath = await SaveFileAsync(salarySlip, uploadDir, $"{userId}_{applicationId}_salary");

            await _db.SaveChangesAsync();

            return ApiResponse<ApplicationReviewDto>.Ok(
                "Tải lên giấy tờ thành công. Vui lòng xem lại thông tin trước khi nộp.",
                BuildReviewDto(app, app.CardType));
        }

        // ════════════════════════════════════════════════════════════════
        //  B4 – Xem lại thông tin đơn
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<ApplicationReviewDto>> GetApplicationReviewAsync(
            int userId, int applicationId)
        {
            var app = await _db.CardApplications
                .Include(a => a.CardType)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

            if (app == null)
                return ApiResponse<ApplicationReviewDto>.Fail("Không tìm thấy đơn đăng ký.");

            return ApiResponse<ApplicationReviewDto>.Ok(
                "Thông tin đơn đăng ký.", BuildReviewDto(app, app.CardType));
        }

        // ════════════════════════════════════════════════════════════════
        //  B5 – Gửi OTP về email hoặc số điện thoại
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<string>> SendApplicationOtpAsync(
            int userId, int applicationId, SendApplicationOtpDto dto)
        {
            if (dto.Type != "email" && dto.Type != "phone")
                return ApiResponse<string>.Fail("Loại kênh nhận OTP không hợp lệ (email hoặc phone).");

            var app = await _db.CardApplications
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

            if (app == null)
                return ApiResponse<string>.Fail("Không tìm thấy đơn đăng ký.");

            if (app.Status != "Draft")
                return ApiResponse<string>.Fail("Đơn đăng ký đã được nộp.");

            if (string.IsNullOrEmpty(app.IdCardPath) || string.IsNullOrEmpty(app.SalarySlipPath))
                return ApiResponse<string>.Fail("Vui lòng tải lên CCCD và bảng lương trước.");

            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return ApiResponse<string>.Fail("Không tìm thấy người dùng.");

            // 1-minute cooldown
            var lastOtp = await _db.OTPs
                .Where(o => o.UserId == userId && o.OTPType == OtpType && !o.IsUsed)
                .OrderByDescending(o => o.ExpiryTime)
                .FirstOrDefaultAsync();

            if (lastOtp != null)
            {
                var createdAt = lastOtp.ExpiryTime.AddMinutes(-5);
                if (createdAt > DateTime.UtcNow.AddMinutes(-1))
                    return ApiResponse<string>.Fail("Vui lòng chờ ít nhất 1 phút trước khi gửi lại OTP.");
            }

            try
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    // Remove old unused OTPs of this type
                    var oldOtps = await _db.OTPs
                        .Where(o => o.UserId == userId && o.OTPType == OtpType && !o.IsUsed)
                        .ToListAsync();
                    _db.OTPs.RemoveRange(oldOtps);

                    var otpCode = "123456"; // 👉 HARD CODE

                    _db.OTPs.Add(new OTP
                    {
                        UserId = userId,
                        OTPCode = otpCode,
                        OTPType = OtpType,
                        ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                        IsUsed = false
                    });

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return ApiResponse<string>.Ok($"OTP demo: 123456");

                    // var otpCode = GenerateSecureOtpCode();
                    // _db.OTPs.Add(new OTP
                    // {
                    //     UserId = userId,
                    //     OTPCode = otpCode,
                    //     OTPType = OtpType,
                    //     ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    //     IsUsed = false
                    // });
                    // await _db.SaveChangesAsync();

                    // if (dto.Type == "email")
                    // {
                    //     await _emailService.SendOtpEmailAsync(user.Email, user.Name, otpCode, OtpType);
                    //     await transaction.CommitAsync();
                    //     return ApiResponse<string>.Ok(
                    //         $"Mã OTP đã được gửi đến email {MaskEmail(user.Email)}. Mã có hiệu lực trong 5 phút.");
                    // }
                    // else // phone
                    // {
                    //     // In production: integrate SMS provider (Twilio, Vonage, etc.)
                    //     _logger.LogInformation("SMS OTP for user {UserId} application {AppId}: {Code}",
                    //         userId, applicationId, otpCode);
                    //     await transaction.CommitAsync();
                    //     return ApiResponse<string>.Ok(
                    //         $"Mã OTP đã được gửi đến số điện thoại {MaskPhone(user.PhoneNumber)}. Mã có hiệu lực trong 5 phút.");
                    // }
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send CardApply OTP for user {UserId}", userId);
                return ApiResponse<string>.Fail("Không thể gửi mã OTP. Vui lòng thử lại.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  B6 – Xác minh OTP và hoàn tất đơn → status = Pending (B7)
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<CardApplicationResponseDto>> FinalizeApplicationAsync(
            int userId, int applicationId, SubmitApplicationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.OtpCode))
                return ApiResponse<CardApplicationResponseDto>.Fail("Vui lòng nhập mã OTP.");

            var app = await _db.CardApplications
                .Include(a => a.CardType)
                .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

            if (app == null)
                return ApiResponse<CardApplicationResponseDto>.Fail("Không tìm thấy đơn đăng ký.");

            if (app.Status != "Draft")
                return ApiResponse<CardApplicationResponseDto>.Fail("Đơn đăng ký đã được nộp trước đó.");

            if (string.IsNullOrEmpty(app.IdCardPath) || string.IsNullOrEmpty(app.SalarySlipPath))
                return ApiResponse<CardApplicationResponseDto>.Fail("Vui lòng tải lên đầy đủ giấy tờ trước khi nộp đơn.");

            // Verify OTP
            var otp = await _db.OTPs.FirstOrDefaultAsync(o =>
                o.UserId == userId &&
                o.OTPCode == dto.OtpCode &&
                o.OTPType == OtpType &&
                !o.IsUsed &&
                o.ExpiryTime > DateTime.UtcNow);

            if (otp == null)
                return ApiResponse<CardApplicationResponseDto>.Fail("Mã OTP không hợp lệ hoặc đã hết hạn.");

            // Mark OTP used + auto-approve application + create card
            otp.IsUsed = true;
            app.Status = "Approved";
            app.ApplicationDate = DateTime.UtcNow;

            // Auto create card from approved application
            var card = await CreateCardFromApplicationAsync(app);

            await _db.SaveChangesAsync();

            return ApiResponse<CardApplicationResponseDto>.Ok(
                "Chúc mừng! Thẻ tín dụng của bạn đã được cấp thành công!",
                new CardApplicationResponseDto
                {
                    ApplicationId = app.Id,
                    CardTypeName = app.CardType.CardName,
                    Status = app.Status,
                    Message = $"Thẻ {app.CardType.CardName} đã được cấp thành công. Số thẻ: {MaskCardNumber(card.CardNumber)}",
                    SubmittedAt = app.ApplicationDate
                });
        }

        // ════════════════════════════════════════════════════════════════
        //  Lấy danh sách đơn đăng ký của người dùng
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<List<CardApplicationResponseDto>>> GetUserApplicationsAsync(int userId)
        {
            var apps = await _db.CardApplications
                .Include(a => a.CardType)
                .Where(a => a.UserId == userId && a.Status != "Draft")
                .OrderByDescending(a => a.ApplicationDate)
                .Select(a => new CardApplicationResponseDto
                {
                    ApplicationId = a.Id,
                    CardTypeName = a.CardType.CardName,
                    Status = a.Status,
                    Message = $"Đơn đăng ký thẻ {a.CardType.CardName}",
                    SubmittedAt = a.ApplicationDate
                })
                .ToListAsync();

            return ApiResponse<List<CardApplicationResponseDto>>.Ok(
                "Lấy danh sách đơn đăng ký thành công.", apps);
        }

        // ════════════════════════════════════════════════════════════════
        //  AUTO CARD CREATION
        // ════════════════════════════════════════════════════════════════

        /// <summary>
        /// Tự động tạo thẻ từ application đã approve
        /// </summary>
        private async Task<Card> CreateCardFromApplicationAsync(CardApplication application)
        {
            // Generate unique card number
            string cardNumber;
            do
            {
                cardNumber = GenerateCardNumber();
            }
            while (await _db.Cards.AnyAsync(c => c.CardNumber == cardNumber));

            var expiryDate = DateOnly.FromDateTime(DateTime.Now.AddYears(5));
            var cvv = GenerateCVV();

            var card = new Card
            {
                CardNumber = cardNumber,
                CVV = cvv,
                ExpiryDate = expiryDate,
                CardStatus = "Active",
                CardTypeId = application.CardTypeId,
                UserId = application.UserId
            };

            _db.Cards.Add(card);
            return card;
        }

        private static string GenerateCardNumber()
        {
            var random = new Random();
            var cardNumber = "4532"; // Visa prefix for demo

            // Generate 12 more digits
            for (int i = 0; i < 12; i++)
            {
                cardNumber += random.Next(0, 10).ToString();
            }

            return cardNumber;
        }

        private static string GenerateCVV()
        {
            var random = new Random();
            return $"{random.Next(100, 1000):D3}";
        }

        // ════════════════════════════════════════════════════════════════
        //  PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════

        private static CardTypeInfoDto MapCardType(CardType ct) => new()
        {
            Id = ct.Id,
            CardName = ct.CardName,
            CardNetwork = ct.CardNetwork,
            CreditLimit = ct.CreditLimit,
            AnnualFee = ct.AnnualFee,
            CashbackRate = ct.CashbackRate,
            Description = ct.Description,
            ImageUrl = ct.ImageUrl ?? string.Empty
        };

        private static ApplicationReviewDto BuildReviewDto(CardApplication app, CardType ct) => new()
        {
            ApplicationId = app.Id,
            Status = app.Status,
            CardType = MapCardType(ct),
            GrossAnnualIncome = app.GrossAnnualIncome,
            IncomeSource = app.IncomeSource,
            Occupation = app.Occupation,
            CompanyName = app.CompanyName,
            IdCardPath = string.IsNullOrEmpty(app.IdCardPath) ? null : app.IdCardPath,
            SalarySlipPath = string.IsNullOrEmpty(app.SalarySlipPath) ? null : app.SalarySlipPath,
            CreatedAt = app.ApplicationDate
        };

        private static (bool ok, string error) ValidateFile(
            IFormFile? file, string fieldName, string[] allowedExts, string[] allowedMimes)
        {
            if (file == null || file.Length == 0)
                return (false, $"{fieldName} là bắt buộc.");

            if (file.Length > MaxFileSizeBytes)
                return (false, $"{fieldName} không được vượt quá 5 MB.");

            var ext = Path.GetExtension(file.FileName?.ToLowerInvariant() ?? "");
            if (string.IsNullOrEmpty(ext) || !allowedExts.Contains(ext))
                return (false, $"{fieldName} chỉ chấp nhận: {string.Join(", ", allowedExts)}.");

            if (!allowedMimes.Contains(file.ContentType?.ToLowerInvariant() ?? ""))
                return (false, $"{fieldName} có định dạng MIME không hợp lệ.");

            return (true, string.Empty);
        }

        private async Task<string> SaveFileAsync(IFormFile file, string directory, string baseName)
        {
            var ext = Path.GetExtension(file.FileName.ToLowerInvariant());
            var fileName = $"{baseName}_{Guid.NewGuid()}{ext}";
            var fullPath = Path.Combine(directory, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Return URL-style relative path from wwwroot, e.g. /uploads/card-applications/5/5_1_idcard_xxx.jpg
            var relPath = fullPath[_env.WebRootPath.Length..].Replace('\\', '/');
            return relPath.StartsWith('/') ? relPath : "/" + relPath;
        }

        private void DeleteFileIfExists(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }

        private static string GenerateSecureOtpCode()
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            var value = BitConverter.ToUInt32(bytes, 0);
            return (100000 + (value % 900000)).ToString();
        }

        private static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4) return cardNumber;
            return "**** **** **** " + cardNumber.Replace(" ", "")[^4..];
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 2) return email;
            return email[..2] + new string('*', at - 2) + email[at..];
        }

        private static string MaskPhone(string phone)
        {
            if (phone.Length < 4) return phone;
            return phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
        }
    }
}
