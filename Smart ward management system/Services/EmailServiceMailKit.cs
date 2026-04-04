using MailKit.Security;
using MimeKit;
using MailKit.Net.Smtp;

namespace Smart_ward_management_system.Services
{
    public class EmailServiceMailKit : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailServiceMailKit> _logger;

        public EmailServiceMailKit(IConfiguration configuration, ILogger<EmailServiceMailKit> logger)
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
                var fromName = emailSettings["FromName"] ?? "Smart Ward Management System";
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var username = emailSettings["Username"] ?? fromAddress;
                var password = emailSettings["FromPassword"];
                var useSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(fromName, fromAddress));
                email.To.Add(new MailboxAddress("", to));
                email.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = StripHtml(body)
                };

                email.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                if (useSsl)
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                }

                await client.AuthenticateAsync(username, password);
                await client.SendAsync(email);
                await client.DisconnectAsync(true);

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
                var fromName = emailSettings["FromName"] ?? "Smart Ward Management System";
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"]);
                var username = emailSettings["Username"] ?? fromAddress;
                var password = emailSettings["FromPassword"];
                var useSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(fromName, fromAddress));
                email.To.Add(new MailboxAddress("", to));
                email.Subject = subject;

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = body,
                    TextBody = StripHtml(body)
                };

                // Add attachment
                bodyBuilder.Attachments.Add(attachmentName, attachment);

                email.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();

                if (useSsl)
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.SslOnConnect);
                }
                else
                {
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                }

                await client.AuthenticateAsync(username, password);
                await client.SendAsync(email);
                await client.DisconnectAsync(true);

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
                foreach (var recipient in recipients)
                {
                    await SendEmailAsync(recipient, subject, body);
                }

                _logger.LogInformation($"Bulk email sent to {recipients.Count} recipients");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk emails");
                throw;
            }
        }

        private string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;
            return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
        }
    }
}
