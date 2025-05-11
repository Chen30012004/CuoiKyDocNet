using System;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CuoiKyDocNet.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            // Kiểm tra email người nhận
            if (string.IsNullOrWhiteSpace(toEmail) || !IsValidEmail(toEmail))
            {
                _logger.LogError("Invalid recipient email address: {ToEmail}", toEmail);
                return false;
            }

            // Kiểm tra subject và body
            if (string.IsNullOrWhiteSpace(subject))
            {
                _logger.LogError("Email subject cannot be empty.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                _logger.LogError("Email body cannot be empty.");
                return false;
            }

            // Lấy cấu hình SMTP từ EmailSettings
            var smtpSettings = _configuration.GetSection("EmailSettings");
            if (smtpSettings == null || !smtpSettings.Exists())
            {
                _logger.LogError("SMTP configuration not found in appsettings.json under 'EmailSettings'.");
                return false;
            }

            var server = smtpSettings["SmtpServer"];
            var portStr = smtpSettings["SmtpPort"];
            var username = smtpSettings["SmtpUsername"];
            var password = smtpSettings["SmtpPassword"];
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];

            // Kiểm tra các giá trị cấu hình
            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(portStr) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError("Missing or invalid SMTP configuration values (SmtpServer, SmtpPort, SmtpUsername, SmtpPassword, SenderEmail).");
                return false;
            }

            if (!int.TryParse(portStr, out int port))
            {
                _logger.LogError("Invalid SMTP port: {Port}", portStr);
                return false;
            }

            if (!IsValidEmail(senderEmail))
            {
                _logger.LogError("Invalid sender email address: {SenderEmail}", senderEmail);
                return false;
            }

            try
            {
                using var smtpClient = new SmtpClient(server)
                {
                    Port = port,
                    Credentials = new System.Net.NetworkCredential(username, password),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName ?? "Podcastify"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
                return true;
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error while sending email to {ToEmail}", toEmail);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                return false;
            }
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}