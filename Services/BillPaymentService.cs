using System.Security.Cryptography;
using backend.Data;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class BillPaymentService : IBillPaymentService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BillPaymentService> _logger;

        public BillPaymentService(
            AppDbContext db,
            ILogger<BillPaymentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ════════════════════════════════════════════════════════════════
        //  B1: LẤY DANH SÁCH LOẠI HÓA ĐƠN
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<List<BillCategoryDto>>> GetBillCategoriesAsync()
        {
            var categories = await _db.BillCategories
                .Where(bc => bc.IsActive)
                .OrderBy(bc => bc.DisplayOrder)
                .ToListAsync();

            var result = categories.Select(bc => new BillCategoryDto
            {
                Id = bc.Id,
                CategoryName = bc.CategoryName,
                CategoryCode = bc.CategoryCode,
                IconUrl = bc.IconUrl,
                DisplayOrder = bc.DisplayOrder
            }).ToList();

            return ApiResponse<List<BillCategoryDto>>.Ok(
                "Lấy danh sách loại hóa đơn thành công.", result);
        }

        // ════════════════════════════════════════════════════════════════
        //  B2: LẤY DANH SÁCH NHÀ CUNG CẤP THEO CATEGORY
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<List<BillProviderDto>>> GetProvidersByCategoryAsync(int categoryId)
        {
            var category = await _db.BillCategories.FindAsync(categoryId);
            if (category == null)
                return ApiResponse<List<BillProviderDto>>.Fail("Không tìm thấy loại hóa đơn.");

            var providers = await _db.BillProviders
                .Include(bp => bp.Category)
                .Where(bp => bp.CategoryId == categoryId && bp.IsActive)
                .OrderBy(bp => bp.DisplayOrder)
                .ToListAsync();

            var result = providers.Select(bp => new BillProviderDto
            {
                Id = bp.Id,
                ProviderName = bp.ProviderName,
                ProviderCode = bp.ProviderCode,
                ServiceFee = bp.ServiceFee,
                LogoUrl = bp.LogoUrl,
                CategoryId = bp.CategoryId,
                CategoryName = bp.Category.CategoryName
            }).ToList();

            return ApiResponse<List<BillProviderDto>>.Ok(
                $"Lấy danh sách nhà cung cấp thành công. Tìm thấy {result.Count} nhà cung cấp.", result);
        }

        // ════════════════════════════════════════════════════════════════
        //  B3: XÁC THỰC MÃ KHÁCH HÀNG VÀ LẤY THÔNG TIN HÓA ĐƠN
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<VerifyCustomerResponse>> VerifyCustomerAsync(VerifyCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CustomerCode))
                return ApiResponse<VerifyCustomerResponse>.Fail("Vui lòng nhập mã khách hàng.");

            var provider = await _db.BillProviders
                .Include(bp => bp.Category)
                .FirstOrDefaultAsync(bp => bp.Id == request.ProviderId);

            if (provider == null)
                return ApiResponse<VerifyCustomerResponse>.Fail("Không tìm thấy nhà cung cấp.");

            // ──────────────────────────────────────────────────────────────
            // MOCK: Simulate external API call to verify customer
            // In production, this would call the provider's API
            // ──────────────────────────────────────────────────────────────

            // Simple validation: customer code must be at least 6 characters
            if (request.CustomerCode.Length < 6)
            {
                return ApiResponse<VerifyCustomerResponse>.Fail(
                    "Mã khách hàng không hợp lệ. Vui lòng kiểm tra lại.");
            }

            // Generate mock bill data based on provider category
            var billAmount = GenerateMockBillAmount(provider.CategoryId);
            var customerName = GenerateMockCustomerName(request.CustomerCode);
            var customerAddress = GenerateMockCustomerAddress();

            var response = new VerifyCustomerResponse
            {
                IsValid = true,
                CustomerCode = request.CustomerCode,
                CustomerName = customerName,
                CustomerAddress = customerAddress,
                BillAmount = billAmount,
                ServiceFee = provider.ServiceFee,
                TotalAmount = billAmount + provider.ServiceFee,
                BillPeriod = $"Tháng {DateTime.UtcNow.Month:D2}/{DateTime.UtcNow.Year}",
                DueDate = DateTime.UtcNow.AddDays(15),
                Provider = new BillProviderDto
                {
                    Id = provider.Id,
                    ProviderName = provider.ProviderName,
                    ProviderCode = provider.ProviderCode,
                    ServiceFee = provider.ServiceFee,
                    LogoUrl = provider.LogoUrl,
                    CategoryId = provider.CategoryId,
                    CategoryName = provider.Category.CategoryName
                }
            };

            return ApiResponse<VerifyCustomerResponse>.Ok(
                "Xác thực mã khách hàng thành công.", response);
        }

        // ════════════════════════════════════════════════════════════════
        //  B5+B6: XỬ LÝ THANH TOÁN VỚI PIN
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<BillPaymentDto>> ProcessPaymentAsync(int userId, ProcessPaymentRequest request)
        {
            using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // ──────────────────────────────────────────────────────────
                // 1. Validate user
                // ──────────────────────────────────────────────────────────
                var user = await _db.Users.FindAsync(userId);
                if (user == null)
                    return ApiResponse<BillPaymentDto>.Fail("Không tìm thấy người dùng.");

                // ──────────────────────────────────────────────────────────
                // 2. Verify PIN
                // ──────────────────────────────────────────────────────────
                if (string.IsNullOrWhiteSpace(user.PinHash))
                    return ApiResponse<BillPaymentDto>.Fail("Bạn chưa thiết lập mã PIN.");

                if (!BCrypt.Net.BCrypt.Verify(request.Pin, user.PinHash))
                    return ApiResponse<BillPaymentDto>.Fail("Mã PIN không chính xác.");

                // ──────────────────────────────────────────────────────────
                // 3. Validate card
                // ──────────────────────────────────────────────────────────
                var card = await _db.Cards
                    .Include(c => c.CardType)
                    .FirstOrDefaultAsync(c => c.Id == request.CardId && c.UserId == userId);

                if (card == null)
                    return ApiResponse<BillPaymentDto>.Fail("Không tìm thấy thẻ.");

                if (card.CardStatus != "Active")
                    return ApiResponse<BillPaymentDto>.Fail("Thẻ không ở trạng thái hoạt động.");

                // Check card balance (credit limit)
                if (card.CardType.CreditLimit < request.TotalAmount)
                    return ApiResponse<BillPaymentDto>.Fail("Hạn mức thẻ không đủ để thanh toán.");

                // ──────────────────────────────────────────────────────────
                // 4. Validate provider
                // ──────────────────────────────────────────────────────────
                var provider = await _db.BillProviders
                    .Include(bp => bp.Category)
                    .FirstOrDefaultAsync(bp => bp.Id == request.ProviderId);

                if (provider == null)
                    return ApiResponse<BillPaymentDto>.Fail("Không tìm thấy nhà cung cấp.");

                // ──────────────────────────────────────────────────────────
                // 5. Create payment record
                // ──────────────────────────────────────────────────────────
                var referenceNumber = GenerateReferenceNumber();
                var now = DateTime.UtcNow;

                var payment = new BillPayment
                {
                    UserId = userId,
                    CardId = card.Id,
                    ProviderId = provider.Id,
                    CustomerCode = request.CustomerCode,
                    CustomerName = request.CustomerName,
                    CustomerAddress = request.CustomerAddress,
                    BillAmount = request.BillAmount,
                    ServiceFee = request.ServiceFee,
                    TotalAmount = request.TotalAmount,
                    Status = "Success",
                    ReferenceNumber = referenceNumber,
                    Description = $"Thanh toán {provider.Category.CategoryName} - {provider.ProviderName}",
                    TransactionDate = now,
                    CreatedAt = now
                };

                _db.BillPayments.Add(payment);
                await _db.SaveChangesAsync();

                // ──────────────────────────────────────────────────────────
                // 6. Create response
                // ──────────────────────────────────────────────────────────
                var result = new BillPaymentDto
                {
                    Id = payment.Id,
                    ReferenceNumber = payment.ReferenceNumber,
                    CustomerCode = payment.CustomerCode,
                    CustomerName = payment.CustomerName,
                    CustomerAddress = payment.CustomerAddress,
                    BillAmount = payment.BillAmount,
                    ServiceFee = payment.ServiceFee,
                    TotalAmount = payment.TotalAmount,
                    Status = payment.Status,
                    TransactionDate = payment.TransactionDate,
                    Provider = new BillProviderDto
                    {
                        Id = provider.Id,
                        ProviderName = provider.ProviderName,
                        ProviderCode = provider.ProviderCode,
                        ServiceFee = provider.ServiceFee,
                        LogoUrl = provider.LogoUrl,
                        CategoryId = provider.CategoryId,
                        CategoryName = provider.Category.CategoryName
                    },
                    CardNumberMasked = MaskCardNumber(card.CardNumber)
                };

                await transaction.CommitAsync();

                _logger.LogInformation(
                    "Bill payment successful. UserId={UserId}, PaymentId={PaymentId}, ReferenceNumber={ReferenceNumber}",
                    userId, payment.Id, referenceNumber);

                return ApiResponse<BillPaymentDto>.Ok(
                    "Thanh toán thành công!", result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error processing bill payment for userId={UserId}", userId);
                return ApiResponse<BillPaymentDto>.Fail("Có lỗi xảy ra khi xử lý thanh toán. Vui lòng thử lại.");
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  LỊCH SỬ THANH TOÁN
        // ════════════════════════════════════════════════════════════════

        public async Task<ApiResponse<List<BillPaymentHistoryDto>>> GetPaymentHistoryAsync(int userId)
        {
            var payments = await _db.BillPayments
                .Include(bp => bp.Provider)
                    .ThenInclude(p => p.Category)
                .Where(bp => bp.UserId == userId)
                .OrderByDescending(bp => bp.TransactionDate)
                .ToListAsync();

            var result = payments.Select(bp => new BillPaymentHistoryDto
            {
                Id = bp.Id,
                ReferenceNumber = bp.ReferenceNumber,
                CustomerCode = bp.CustomerCode,
                ProviderName = bp.Provider.ProviderName,
                CategoryName = bp.Provider.Category.CategoryName,
                TotalAmount = bp.TotalAmount,
                Status = bp.Status,
                TransactionDate = bp.TransactionDate
            }).ToList();

            return ApiResponse<List<BillPaymentHistoryDto>>.Ok(
                $"Lấy lịch sử thanh toán thành công. Tìm thấy {result.Count} giao dịch.", result);
        }

        // ════════════════════════════════════════════════════════════════
        //  HELPER METHODS
        // ════════════════════════════════════════════════════════════════

        private static decimal GenerateMockBillAmount(int categoryId)
        {
            // Generate mock bill amount based on category
            return categoryId switch
            {
                1 => new Random().Next(150000, 500000),  // Electric: 150k-500k
                2 => new Random().Next(50000, 200000),   // Water: 50k-200k
                3 => new Random().Next(150000, 300000),  // Internet: 150k-300k
                4 => new Random().Next(2000000, 5000000), // Tuition: 2M-5M
                5 => new Random().Next(50000, 200000),   // Mobile: 50k-200k
                6 => new Random().Next(100000, 300000),  // TV: 100k-300k
                _ => new Random().Next(100000, 500000)
            };
        }

        private static string GenerateMockCustomerName(string customerCode)
        {
            // Generate mock name based on customer code
            var lastDigit = int.Parse(customerCode.Substring(customerCode.Length - 1, 1));
            string[] names = {
                "Nguyễn Văn An", "Trần Thị Bình", "Lê Văn Cường",
                "Phạm Thị Dung", "Hoàng Văn Em", "Võ Thị Phương",
                "Đặng Văn Giang", "Bùi Thị Hoa", "Ngô Văn Inh",
                "Dương Thị Khánh"
            };
            return names[lastDigit % names.Length];
        }

        private static string GenerateMockCustomerAddress()
        {
            string[] addresses = {
                "12 Trần Hưng Đạo, P.1, Q.5, TP.HCM",
                "45 Nguyễn Huệ, P.Bến Nghé, Q.1, TP.HCM",
                "78 Lê Lợi, P.Bến Thành, Q.1, TP.HCM",
                "23 Võ Văn Tần, P.6, Q.3, TP.HCM",
                "56 Điện Biên Phủ, P.Đakao, Q.1, TP.HCM",
                "89 Cách Mạng Tháng 8, P.6, Q.Tân Bình, TP.HCM",
                "34 Hai Bà Trưng, P.Bến Nghé, Q.1, TP.HCM",
                "67 Lý Tự Trọng, P.Bến Nghé, Q.1, TP.HCM"
            };
            return addresses[new Random().Next(addresses.Length)];
        }

        private static string GenerateReferenceNumber()
        {
            // Format: BPyyyyMMddHHmmssfff + 4 random digits
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            var randomDigits = RandomNumberGenerator.GetInt32(1000, 9999);
            return $"BP{timestamp}{randomDigits}";
        }

        private static string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return cardNumber;

            var last4 = cardNumber.Substring(cardNumber.Length - 4);
            return $"**** **** **** {last4}";
        }
    }
}
