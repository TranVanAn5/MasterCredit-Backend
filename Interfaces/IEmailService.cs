namespace backend.Interfaces
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string toEmail, string toName, string otpCode, string otpType);
    }
}
