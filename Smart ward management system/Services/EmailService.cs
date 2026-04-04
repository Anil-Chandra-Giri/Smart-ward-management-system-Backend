using System.Net;
using System.Net.Mail;

namespace Smart_ward_management_system.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromAddress = emailSettings["FromAddress"];
                var fromPassword = emailSettings["FromPassword"];
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, emailSettings["FromName"] ?? "Smart Ward Management System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                    Priority = MailPriority.Normal
                };

                message.To.Add(to);

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(fromAddress, fromPassword),
                    Timeout = 30000 // 30 seconds timeout
                };

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email to {to}");
                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromAddress = emailSettings["FromAddress"];
                var fromPassword = emailSettings["FromPassword"];
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                using var message = new MailMessage
                {
                    From = new MailAddress(fromAddress, emailSettings["FromName"] ?? "Smart Ward Management System"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                message.To.Add(to);

                // Add attachment
                using var stream = new MemoryStream(attachment);
                var mailAttachment = new Attachment(stream, attachmentName);
                message.Attachments.Add(mailAttachment);

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(fromAddress, fromPassword),
                    Timeout = 30000
                };

                await client.SendMailAsync(message);
                _logger.LogInformation($"Email with attachment sent successfully to {to}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email with attachment to {to}");
                throw;
            }
        }

        public async Task SendBulkEmailAsync(List<string> recipients, string subject, string body)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var fromAddress = emailSettings["FromAddress"];
                var fromPassword = emailSettings["FromPassword"];
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                using var client = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(fromAddress, fromPassword),
                    Timeout = 30000
                };

                foreach (var recipient in recipients)
                {
                    using var message = new MailMessage
                    {
                        From = new MailAddress(fromAddress, emailSettings["FromName"] ?? "Smart Ward Management System"),
                        Subject = subject,
                        Body = body,
                        IsBodyHtml = true
                    };

                    message.To.Add(recipient);
                    await client.SendMailAsync(message);
                }

                _logger.LogInformation($"Bulk email sent to {recipients.Count} recipients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk emails");
                throw;
            }
        }
    }
}
