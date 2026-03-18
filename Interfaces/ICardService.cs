using backend.DTOs;
using Microsoft.AspNetCore.Http;

namespace backend.Interfaces
{
    public interface ICardService
    {
        // ── Catalog ──────────────────────────────────────────────────────
        Task<ApiResponse<List<CardDto>>> GetUserCardsAsync(int userId);
        Task<ApiResponse<CardDetailDto>> GetCardDetailAsync(int userId, int cardId);
        Task<ApiResponse<List<CardTypeInfoDto>>> GetAllCardTypesAsync();
        Task<ApiResponse<List<CardTypeInfoDto>>> CompareCardTypesAsync(List<int> cardTypeIds);
        Task<ApiResponse<CardTypeInfoDto>> GetCardTypeByIdAsync(int cardTypeId);

        // ── Card Application – Multi-step flow ───────────────────────────
        /// <summary>B2: Tạo đơn nháp với thông tin tài chính + loại thẻ</summary>
        Task<ApiResponse<ApplicationReviewDto>> StartApplicationAsync(int userId, StartApplicationDto dto);

        /// <summary>B3: Upload CCCD và bảng lương cho đơn nháp</summary>
        Task<ApiResponse<ApplicationReviewDto>> UploadDocumentsAsync(
            int userId, int applicationId, IFormFile idCard, IFormFile salarySlip);

        /// <summary>B4: Xem lại toàn bộ thông tin đơn trước khi nộp</summary>
        Task<ApiResponse<ApplicationReviewDto>> GetApplicationReviewAsync(int userId, int applicationId);

        /// <summary>B5: Gửi OTP về email hoặc số điện thoại</summary>
        Task<ApiResponse<string>> SendApplicationOtpAsync(
            int userId, int applicationId, SendApplicationOtpDto dto);

        /// <summary>B6: Xác minh OTP và hoàn tất đơn (chuyển sang Pending)</summary>
        Task<ApiResponse<CardApplicationResponseDto>> FinalizeApplicationAsync(
            int userId, int applicationId, SubmitApplicationDto dto);

        /// <summary>Lấy danh sách đơn đăng ký của người dùng</summary>
        Task<ApiResponse<List<CardApplicationResponseDto>>> GetUserApplicationsAsync(int userId);
    }
}
