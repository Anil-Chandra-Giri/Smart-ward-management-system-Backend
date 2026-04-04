using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;

namespace Smart_ward_management_system.Common
{
    // Services/ISmsService.cs
    public interface ISmsService
    {
        Task<bool> SendBulkSmsAsync(List<string> phoneNumbers, string message);
        Task<bool> SendDisasterAlertAsync(string disasterName, string location, string severity);
    }

    // Services/SparrowSmsService.cs
    public class SparrowSmsService : ISmsService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SparrowSmsService> _logger;
        private readonly ApplicationDbContext _dbContext;

        public SparrowSmsService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<SparrowSmsService> logger,
            ApplicationDbContext dbContext)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<bool> SendBulkSmsAsync(List<string> phoneNumbers, string message)
        {
            try
            {
                if (phoneNumbers == null || !phoneNumbers.Any())
                {
                    _logger.LogWarning("No phone numbers to send SMS to");
                    return false;
                }

                var token = _configuration["SparrowSMS:Token"];
                Console.WriteLine(token);
                var from = _configuration["SparrowSMS:from"]; 

                
                var validNumbers = phoneNumbers
                    .Where(p => !string.IsNullOrWhiteSpace(p) && p.Length == 10 && p.StartsWith("9"))
                    .ToList();

                if (!validNumbers.Any())
                {
                    _logger.LogWarning("No valid phone numbers found");
                    return false;
                }

                var recipients = string.Join(",", validNumbers);

                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("token", token),
                new KeyValuePair<string, string>("from", from),
                new KeyValuePair<string, string>("to", recipients),
                new KeyValuePair<string, string>("text", message)
            });

                _logger.LogInformation($"Sending SMS to {validNumbers.Count} recipients");

                var response = await _httpClient.PostAsync("https://api.sparrowsms.com/v2/sms/", content);
                var responseString = await response.Content.ReadAsStringAsync();

                _logger.LogInformation($"SMS API Response: {responseString}");

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send bulk SMS");
                return false;
            }
        }

        public async Task<bool> SendDisasterAlertAsync(string disasterName, string location, string severity)
        {
            try
            {
                var message = $"⚠️ DISASTER ALERT: {disasterName} reported in {location}. " +
                             $"Severity: {severity}. Please take necessary precautions. " +
                             $"For help, contact local authorities.";

                var phoneNumbers = await GetRegisteredPhoneNumbers();

                if (!phoneNumbers.Any())
                {
                    _logger.LogWarning("No registered volunteers to send disaster alert to");
                    return false;
                }

                return await SendBulkSmsAsync(phoneNumbers, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendDisasterAlertAsync");
                return false;
            }
        }

        private async Task<List<string>> GetRegisteredPhoneNumbers()
        {
            // Ensure _dbContext is not null
            if (_dbContext == null)
            {
                _logger.LogError("DbContext is null in GetRegisteredPhoneNumbers");
                return new List<string>();
            }

            return await _dbContext.Users
                .Select(v => v.PhoneNumber)
                .ToListAsync();
        }
    }
}
