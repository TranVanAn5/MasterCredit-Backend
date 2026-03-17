using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using backend.Interfaces;

namespace backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string toName, string otpCode, string otpType)
        {
            var smtpHost = _config["Email:SmtpHost"]!;
            var smtpPort = int.Parse(_config["Email:SmtpPort"]!);
            var smtpUser = _config["Email:SmtpUser"]!;
            var smtpPass = _config["Email:SmtpPass"]!;
            var fromEmail = _config["Email:FromEmail"]!;
            var fromName = _config["Email:FromName"]!;

            var subject = otpType == "Login"
                ? "MasterCredit - Mã OTP đăng nhập"
                : "MasterCredit - Mã OTP xác thực tài khoản";

            var actionText = otpType == "Login" ? "đăng nhập" : "đăng ký tài khoản";

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 32px;'>
                    <h2 style='color: #1a237e; text-align: center;'>MasterCredit</h2>
                    <p>Xin chào <strong>{toName}</strong>,</p>
                    <p>Bạn vừa yêu cầu {actionText}. Vui lòng sử dụng mã OTP bên dưới để xác thực:</p>
                    <div style='text-align: center; margin: 32px 0;'>
                        <span style='font-size: 40px; font-weight: bold; letter-spacing: 12px; color: #1a237e; background: #f0f4ff; padding: 16px 32px; border-radius: 8px;'>
                            {otpCode}
                        </span>
                    </div>
                    <p style='color: #f44336;'><strong>Lưu ý:</strong> Mã OTP có hiệu lực trong <strong>5 phút</strong> và chỉ sử dụng được một lần.</p>
                    <p>Nếu bạn không yêu cầu mã này, vui lòng bỏ qua email này.</p>
                    <hr style='border: none; border-top: 1px solid #e0e0e0; margin: 24px 0;'/>
                    <p style='color: #9e9e9e; font-size: 12px; text-align: center;'>
                        © {DateTime.Now.Year} MasterCredit. Không trả lời email này.
                    </p>
                </div>";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlBody };

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPass);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
