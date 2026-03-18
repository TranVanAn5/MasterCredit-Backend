using backend.DTOs;

namespace backend.Interfaces
{
    public interface IBillPaymentService
    {
        // ── Bill Payment – Multi-step flow ───────────────────────────

        /// <summary>B1: Lấy danh sách loại hóa đơn (điện, nước, wifi, học phí...)</summary>
        Task<ApiResponse<List<BillCategoryDto>>> GetBillCategoriesAsync();

        /// <summary>B2: Lấy danh sách nhà cung cấp theo category</summary>
        Task<ApiResponse<List<BillProviderDto>>> GetProvidersByCategoryAsync(int categoryId);

        /// <summary>B3: Xác thực mã khách hàng và lấy thông tin hóa đơn</summary>
        Task<ApiResponse<VerifyCustomerResponse>> VerifyCustomerAsync(VerifyCustomerRequest request);

        /// <summary>
        /// B4: Chọn thẻ để thanh toán (sử dụng ICardService.GetUserCardsAsync)
        /// </summary>

        /// <summary>B5+B6: Xử lý thanh toán với PIN và trả về kết quả</summary>
        Task<ApiResponse<BillPaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request);

        /// <summary>Lấy lịch sử thanh toán hóa đơn của người dùng</summary>
        Task<ApiResponse<List<BillPaymentHistoryDto>>> GetPaymentHistoryAsync(int userId);
    }
}
