// Services/LoggingService.cs
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Models;
using System.Text.Json;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Services
{
    public class LoggingService : ILoggingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoggingService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(LogEntry logEntry)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                // Get IP address
                logEntry.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                logEntry.RequestPath = httpContext.Request.Path;

                // Get user information from JWT claims (NOT from session)
                var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
                if (Guid.TryParse(userIdClaim, out Guid userId))
                {
                    logEntry.UserId = userId;
                }

                var userNameClaim = httpContext.User.FindFirst("UserName")?.Value;
                if (!string.IsNullOrEmpty(userNameClaim))
                {
                    logEntry.UserName = userNameClaim;
                }

                // Get role from JWT claims
                var roleClaim = httpContext.User.FindFirst("Role")?.Value;
                if (!string.IsNullOrEmpty(roleClaim))
                {
                    logEntry.Department = roleClaim;
                }

                // Get ward number from JWT claims (NOT from session)
                var wardNumberClaim = httpContext.User.FindFirst("WardNumber")?.Value;
                if (!string.IsNullOrEmpty(wardNumberClaim))
                {
                    logEntry.WardNumber = wardNumberClaim;
                }

                // Get email from JWT claims
                var emailClaim = httpContext.User.FindFirst("Email")?.Value;
                if (!string.IsNullOrEmpty(emailClaim))
                {
                    // Store email as additional data or in a separate field
                    if (logEntry.AdditionalData == null)
                        logEntry.AdditionalData = "{}";

                    var additionalData = JsonSerializer.Deserialize<Dictionary<string, object>>(logEntry.AdditionalData) ?? new Dictionary<string, object>();
                    additionalData["Email"] = emailClaim;
                    logEntry.AdditionalData = JsonSerializer.Serialize(additionalData);
                }
            }

            _context.Logs.Add(logEntry);
            await _context.SaveChangesAsync();
        }

        public async Task LogInfoAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Information,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };
            await LogAsync(logEntry);
        }

        public async Task LogWarningAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Warning,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };
            await LogAsync(logEntry);
        }

        public async Task LogErrorAsync(string message, Exception? ex = null, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Error,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ExceptionDetails = ex?.ToString(),
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };
            await LogAsync(logEntry);
        }

        public async Task LogDebugAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Debug,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };
            await LogAsync(logEntry);
        }

        public async Task LogCriticalAsync(string message, Exception? ex = null, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Critical,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ExceptionDetails = ex?.ToString(),
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            };
            await LogAsync(logEntry);
        }

        // Citizen Services
        public async Task LogCitizenActionAsync(string citizenId, string action, string? department = null)
        {
            await LogInfoAsync(
                $"Citizen {citizenId}: {action}",
                LogCategory.CitizenServices,
                new { CitizenId = citizenId, Action = action, Department = department }
            );
        }

        public async Task LogCertificateRequestAsync(string requestType, string citizenId, string requestId, string action)
        {
            await LogInfoAsync(
                $"Certificate Request - {requestType}: {action}",
                LogCategory.DocumentVerification,
                new { RequestType = requestType, CitizenId = citizenId, RequestId = requestId, Action = action }
            );
        }

        // Complaint/Grievance
        public async Task LogComplaintAsync(Guid complaintId, string action, string status)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.Grievance,
                Message = $"Complaint #{complaintId}: {action}",
                ComplaintId = complaintId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { Action = action, Status = status })
            };
            await LogAsync(logEntry);
        }

        public async Task LogComplaintEscalationAsync(Guid complaintId, int fromLevel, int toLevel)
        {
            await LogWarningAsync(
                $"Complaint #{complaintId} escalated from level {fromLevel} to {toLevel}",
                LogCategory.Grievance,
                new { ComplaintId = complaintId, FromLevel = fromLevel, ToLevel = toLevel }
            );
        }

        // Payments
        public async Task LogPaymentAsync(int paymentId, string citizenId, decimal amount, string paymentType, string status)
        {
            var logEntry = new LogEntry
            {
                Level = status == "Success" ? LogLevel.Information : LogLevel.Warning,
                Category = LogCategory.TaxCollection,
                Message = $"Payment {paymentId}: {paymentType} of ₹{amount:N2} - {status}",
                PaymentId = paymentId,
                CitizenId = citizenId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { Amount = amount, PaymentType = paymentType, Status = status })
            };
            await LogAsync(logEntry);
        }

        // Service Requests
        public async Task LogServiceRequestAsync(Guid serviceRequestId, string serviceType, string action, string status)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.ServiceRequests,
                Message = $"Service Request #{serviceRequestId} ({serviceType}): {action}",
                ServiceRequestId = serviceRequestId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { ServiceType = serviceType, Action = action, Status = status })
            };
            await LogAsync(logEntry);
        }

        // Waste Management
        public async Task LogWasteCollectionAsync(string routeId, string vehicleId, string action)
        {
            await LogInfoAsync(
                $"Waste Collection - {action}",
                LogCategory.WasteManagement,
                new { RouteId = routeId, VehicleId = vehicleId, Action = action }
            );
        }

        // Appointments & Queue
        public async Task LogAppointmentAsync(Guid appointmentId, string citizenId, string action)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.Appointments,
                Message = $"Appointment #{appointmentId}: {action}",
                AppointmentId = appointmentId,
                CitizenId = citizenId,
                Timestamp = DateTime.UtcNow
            };
            await LogAsync(logEntry);
        }

        public async Task LogQueueActionAsync(int queueId, int tokenNumber, string action)
        {
            await LogInfoAsync(
                $"Queue #{queueId}: Token {tokenNumber} - {action}",
                LogCategory.Appointments,
                new { QueueId = queueId, TokenNumber = tokenNumber, Action = action }
            );
        }

        // Polls & Notices
        public async Task LogPollActionAsync(Guid pollId, string action)
        {
            var logEntry = new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.Polls,
                Message = $"Poll #{pollId}: {action}",
                PollId = pollId,
                Timestamp = DateTime.UtcNow
            };
            await LogAsync(logEntry);
        }

        public async Task LogNoticeActionAsync(Guid noticeId, string action)
        {
            await LogInfoAsync(
                $"Notice #{noticeId}: {action}",
                LogCategory.Notifications,
                new { NoticeId = noticeId, Action = action }
            );
        }

        // Document Verification
        public async Task LogDocumentVerificationAsync(Guid documentId, string citizenId, bool isVerified, string? remarks = null)
        {
            var status = isVerified ? "Verified" : "Rejected";
            var logEntry = new LogEntry
            {
                Level = isVerified ? LogLevel.Information : LogLevel.Warning,
                Category = LogCategory.DocumentVerification,
                Message = $"Document #{documentId} {status} for citizen {citizenId}",
                DocumentId = documentId,
                CitizenId = citizenId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { IsVerified = isVerified, Remarks = remarks })
            };
            await LogAsync(logEntry);
        }

        // User Management
        public async Task LogUserActionAsync(Guid userId, string action, string? details = null)
        {
            await LogInfoAsync(
                $"User {userId}: {action}",
                LogCategory.UserManagement,
                new { UserId = userId, Action = action, Details = details }
            );
        }

        // Query methods
        public async Task<(IEnumerable<LogEntry> logs, int totalCount)> GetLogsAsync(LogQueryFilter filter)
        {
            var query = _context.Logs.AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.Timestamp >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(l => l.Timestamp <= filter.EndDate.Value);
            if (filter.Level.HasValue)
                query = query.Where(l => l.Level == filter.Level.Value);
            if (filter.Category.HasValue)
                query = query.Where(l => l.Category == filter.Category.Value);
            if (filter.UserId.HasValue)
                query = query.Where(l => l.UserId == filter.UserId.Value);
            if (!string.IsNullOrEmpty(filter.CitizenId))
                query = query.Where(l => l.CitizenId == filter.CitizenId);
            if (!string.IsNullOrEmpty(filter.WardNumber))
                query = query.Where(l => l.WardNumber == filter.WardNumber);
            if (!string.IsNullOrEmpty(filter.Department))
                query = query.Where(l => l.Department == filter.Department);
            if (filter.ComplaintId.HasValue)
                query = query.Where(l => l.ComplaintId == filter.ComplaintId.Value);
            if (filter.ServiceRequestId.HasValue)
                query = query.Where(l => l.ServiceRequestId == filter.ServiceRequestId.Value);
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(l =>
                    l.Message.Contains(filter.SearchTerm) ||
                    (l.AdditionalData != null && l.AdditionalData.Contains(filter.SearchTerm)));
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<LogEntry?> GetLogByIdAsync(int id)
        {
            return await _context.Logs.FindAsync(id);
        }

        public async Task<LogDashboardViewModel> GetDashboardStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var last24h = DateTime.UtcNow.AddHours(-24);

            var recentLogs = await _context.Logs
                .Where(l => l.Timestamp >= last24h)
                .ToListAsync();

            var todayLogs = await _context.Logs
                .Where(l => l.Timestamp.Date == today)
                .ToListAsync();

            return new LogDashboardViewModel
            {
                TotalLogsToday = todayLogs.Count,
                ErrorCount24h = recentLogs.Count(l => l.Level == LogLevel.Error),
                WarningCount24h = recentLogs.Count(l => l.Level == LogLevel.Warning),
                InfoCount24h = recentLogs.Count(l => l.Level == LogLevel.Information),
                CitizenServiceRequests = recentLogs.Count(l => l.Category == LogCategory.CitizenServices),
                GrievancesFiled = recentLogs.Count(l => l.Category == LogCategory.Grievance && l.Message.Contains("filed")),
                GrievancesResolved = recentLogs.Count(l => l.Category == LogCategory.Grievance && l.Message.Contains("resolved")),
                LogsByCategory = recentLogs.GroupBy(l => l.Category).ToDictionary(g => g.Key, g => g.Count()),
                RecentErrors = recentLogs.Where(l => l.Level == LogLevel.Error).Take(10).ToList(),
                RecentCitizenActions = recentLogs.Where(l => l.Category == LogCategory.CitizenServices).Take(10).ToList(),
                TopActiveDepartments = recentLogs
                    .Where(l => l.Department != null)
                    .GroupBy(l => l.Department!)
                    .ToDictionary(g => g.Key, g => g.Count())
                    .OrderByDescending(x => x.Value)
                    .Take(5)
                    .ToDictionary(x => x.Key, x => x.Value)
            };
        }

        public async Task<IEnumerable<LogEntry>> GetEntityLogsAsync(string entityType, Guid entityId)
        {
            var query = _context.Logs.AsQueryable();

            switch (entityType.ToLower())
            {
                case "complaint":
                    query = query.Where(l => l.ComplaintId == entityId);
                    break;
                case "servicerequest":
                    query = query.Where(l => l.ServiceRequestId == entityId);
                    break;
                default:
                    return new List<LogEntry>();
            }

            return await query
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();
        }

        // Export methods
        public async Task<byte[]> ExportLogsToCsvAsync(LogQueryFilter filter)
        {
            // Implementation here
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ExportLogsToExcelAsync(LogQueryFilter filter)
        {
            // Implementation here
            return Array.Empty<byte>();
        }

        public async Task<string> ExportLogsToJsonAsync(LogQueryFilter filter)
        {
            // Implementation here
            return string.Empty;
        }

        public async Task<Dictionary<LogCategory, int>> GetCategoryWiseCountAsync(DateTime startDate, DateTime endDate)
        {
            var result = await _context.Logs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => l.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Category, g => g.Count);

            return result;
        }

        public async Task<Dictionary<LogLevel, int>> GetLevelWiseCountAsync(DateTime startDate, DateTime endDate)
        {
            var result = await _context.Logs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Level, g => g.Count);

            return result;
        }

        public async Task<int> ClearOldLogsAsync(DateTime cutoffDate)
        {
            var oldLogs = _context.Logs.Where(l => l.Timestamp < cutoffDate);
            var count = await oldLogs.CountAsync();

            if (count > 0)
            {
                _context.Logs.RemoveRange(oldLogs);
                await _context.SaveChangesAsync();
            }

            return count;
        }
    }
}