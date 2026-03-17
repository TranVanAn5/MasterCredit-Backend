using backend.DTOs;

namespace backend.Interfaces
{
    public interface IAuthService
    {
        // Register flow
        Task<ApiResponse<object>> RegisterAsync(RegisterDto dto);
        Task<ApiResponse<object>> VerifyRegisterOtpAsync(VerifyOtpDto dto);
        Task<ApiResponse<object>> UploadCitizenIdAsync(UploadCitizenIdDto dto);
        Task<ApiResponse<object>> SetPinAsync(SetPinDto dto);

        // Login flow
        Task<ApiResponse<object>> LoginAsync(LoginDto dto);
        Task<ApiResponse<LoginResponseDto>> VerifyLoginOtpAsync(VerifyOtpDto dto);

        // Shared
        Task<ApiResponse<object>> ResendOtpAsync(ResendOtpDto dto);
    }
}
