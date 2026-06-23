using System.Net;
using System.Net.Mail;

namespace Smart_ward_management_system.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        private SmtpClient CreateSmtpClient()
        {
            var settings = _configuration.GetSection("EmailSettings");

            var smtpServer = settings["SmtpServer"];
            var port = int.Parse(settings["Port"]!);
            var username = settings["Username"];
            var password = settings["Password"];
            var enableSsl = bool.Parse(settings["EnableSSL"] ?? "true");

            return new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(username, password),
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 30000
            };
        }

        public async Task SendEmailAsync(
            string to,
            string subject,
            string body)
        {
            try
            {
                var settings = _configuration.GetSection("EmailSettings");

                using var message = new MailMessage
                {
                    From = new MailAddress(
                        settings["FromEmail"]!,
                        settings["FromName"]),

                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                message.To.Add(to);

                using var client = CreateSmtpClient();

                await client.SendMailAsync(message);

                _logger.LogInformation(
                    "Email sent successfully to {Email}",
                    to);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send email to {Email}",
                    to);

                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(
            string to,
            string subject,
            string body,
            byte[] attachment,
            string attachmentName)
        {
            try
            {
                var settings = _configuration.GetSection("EmailSettings");

                using var message = new MailMessage
                {
                    From = new MailAddress(
                        settings["FromEmail"]!,
                        settings["FromName"]),

                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                using var stream = new MemoryStream(attachment);

                message.Attachments.Add(
                    new Attachment(stream, attachmentName));

                using var client = CreateSmtpClient();

                await client.SendMailAsync(message);

                _logger.LogInformation(
                    "Email with attachment sent to {Email}",
                    to);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send attachment email to {Email}",
                    to);

                throw;
            }
        }

        public async Task SendBulkEmailAsync(
            List<string> recipients,
            string subject,
            string body)
        {
            foreach (var email in recipients)
            {
                await SendEmailAsync(email, subject, body);
            }

            _logger.LogInformation(
                "Bulk email sent to {Count} recipients",
                recipients.Count);
        }
    }
}