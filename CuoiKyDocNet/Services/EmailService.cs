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

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            if (smtpSettings == null || !smtpSettings.Exists())
            {
                _logger.LogError("SMTP configuration not found in appsettings.json.");
                throw new InvalidOperationException("SMTP configuration not found in appsettings.json.");
            }

            var server = smtpSettings["Server"];
            var portStr = smtpSettings["Port"];
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var senderEmail = smtpSettings["SenderEmail"];
            var senderName = smtpSettings["SenderName"];

            if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(portStr) ||
                string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(senderEmail))
            {
                _logger.LogError("Missing or invalid SMTP configuration values (Server, Port, Username, Password, SenderEmail).");
                throw new InvalidOperationException("Missing or invalid SMTP configuration values.");
            }

            if (!int.TryParse(portStr, out int port))
            {
                _logger.LogError("Invalid SMTP port: {Port}", portStr);
                throw new InvalidOperationException("Invalid SMTP port.");
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
            }
            catch (SmtpException ex)
            {
                _logger.LogError(ex, "SMTP error while sending email to {ToEmail}", toEmail);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
                throw;
            }
        }
    }
}