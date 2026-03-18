namespace backend.DTOs
{
    // ─────────────────────────────────────────────────────────
    // BILL PAYMENT - MULTI-STEP FLOW
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// B1: Danh sách loại hóa đơn (điện, nước, wifi, học phí...)
    /// GET /api/bill-payment/categories
    /// </summary>
    public class BillCategoryDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string CategoryCode { get; set; } = string.Empty;
        public string IconUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// B2: Danh sách nhà cung cấp theo category
    /// GET /api/bill-payment/categories/{categoryId}/providers
    /// </summary>
    public class BillProviderDto
    {
        public int Id { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderCode { get; set; } = string.Empty;
        public decimal ServiceFee { get; set; }
        public string LogoUrl { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    /// <summary>
    /// B3: Xác thực mã khách hàng và lấy thông tin hóa đơn
    /// POST /api/bill-payment/verify-customer
    /// </summary>
    public class VerifyCustomerRequest
    {
        public int ProviderId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// B3: Response thông tin hóa đơn sau khi verify
    /// </summary>
    public class VerifyCustomerResponse
    {
        public bool IsValid { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public decimal BillAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string BillPeriod { get; set; } = string.Empty;      // e.g. "Tháng 03/2026"
        public DateTime DueDate { get; set; }
        public BillProviderDto Provider { get; set; } = null!;
    }

    /// <summary>
    /// B4: Danh sách thẻ để chọn (sử dụng CardDto đã có)
    /// GET /api/cards
    /// </summary>

    /// <summary>
    /// B5: Xử lý thanh toán với PIN
    /// POST /api/bill-payment/process
    /// </summary>
    public class ProcessPaymentRequest
    {
        public int ProviderId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public int CardId { get; set; }
        public string Pin { get; set; } = string.Empty;
        public decimal BillAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
    }

    /// <summary>
    /// B6: Response thanh toán thành công
    /// </summary>
    public class BillPaymentDto
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerAddress { get; set; } = string.Empty;
        public decimal BillAmount { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public BillProviderDto Provider { get; set; } = null!;
        public string CardNumberMasked { get; set; } = string.Empty;  // **** **** **** XXXX
    }

    /// <summary>
    /// Lịch sử thanh toán hóa đơn
    /// GET /api/bill-payment/history
    /// </summary>
    public class BillPaymentHistoryDto
    {
        public int Id { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string CustomerCode { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
    }
}
