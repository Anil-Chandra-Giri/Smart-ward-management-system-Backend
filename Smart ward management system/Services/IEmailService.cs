namespace Smart_ward_management_system.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task SendEmailWithAttachmentAsync(string to, string subject, string body, byte[] attachment, string attachmentName);
        Task SendBulkEmailAsync(List<string> recipients, string subject, string body);
    }
}
