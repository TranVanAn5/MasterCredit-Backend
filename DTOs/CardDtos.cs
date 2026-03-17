namespace backend.DTOs
{
    public class CardTypeInfoDto
    {
        public int Id { get; set; }
        public string CardName { get; set; } = string.Empty;
        public string CardNetwork { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public decimal AnnualFee { get; set; }
        public decimal CashbackRate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class CardDto
    {
        public int Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;  // masked: **** **** **** XXXX
        public string ExpiryDate { get; set; } = string.Empty;  // MM/YY
        public string CardStatus { get; set; } = string.Empty;
        public string HolderName { get; set; } = string.Empty;
        public CardTypeInfoDto CardType { get; set; } = null!;
    }

    // ─────────────────────────────────────────────────────────
    // CARD APPLICATION - MULTI-STEP FLOW
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// B1 already uses GET /api/cards/types (CardTypeInfoDto above).
    /// B2: Gửi thông tin tài chính + chọn loại thẻ → tạo đơn nháp (Draft)
    /// </summary>
    public class StartApplicationDto
    {
        public int CardTypeId { get; set; }
        public decimal GrossAnnualIncome { get; set; }
        public string IncomeSource { get; set; } = string.Empty;   // e.g. "Lương", "Kinh doanh"
        public string Occupation { get; set; } = string.Empty;     // e.g. "Kỹ sư phần mềm"
        public string CompanyName { get; set; } = string.Empty;
    }

    /// <summary>
    /// B4: Xem lại toàn bộ thông tin đơn trước khi nộp
    /// </summary>
    public class ApplicationReviewDto
    {
        public int ApplicationId { get; set; }
        public string Status { get; set; } = string.Empty;         // Draft / Pending / Approved / Rejected
        public CardTypeInfoDto CardType { get; set; } = null!;
        public decimal GrossAnnualIncome { get; set; }
        public string IncomeSource { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? IdCardPath { get; set; }                    // null nếu chưa upload
        public string? SalarySlipPath { get; set; }                // null nếu chưa upload
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// B5: Chọn kênh nhận OTP (email hoặc phone)
    /// </summary>
    public class SendApplicationOtpDto
    {
        public string Type { get; set; } = string.Empty;           // "email" or "phone"
    }

    /// <summary>
    /// B6: Nhập mã OTP để hoàn tất đơn
    /// </summary>
    public class SubmitApplicationDto
    {
        public string OtpCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response sau khi đơn đã được gửi thành công (B7)
    /// </summary>
    public class CardApplicationResponseDto
    {
        public int ApplicationId { get; set; }
        public string CardTypeName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;         // "Pending"
        public string Message { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
    }

    // ─────────────────────────────────────────────────────────
    // (Legacy – kept for backward compatibility)
    // ─────────────────────────────────────────────────────────

    public class CardApplicationRequestDto
    {
        public int CardTypeId { get; set; }
        public decimal GrossAnnualIncome { get; set; }
        public string IncomeSource { get; set; } = string.Empty;
        public string Occupation { get; set; } = string.Empty;
        public string IdCardPath { get; set; } = string.Empty;
        public string SalarySlipPath { get; set; } = string.Empty;
    }

    public class SendOtpRequestDto
    {
        public string Destination { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    public class VerifyOtpRequestDto
    {
        public string Destination { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public CardApplicationRequestDto ApplicationData { get; set; } = null!;
    }
}
