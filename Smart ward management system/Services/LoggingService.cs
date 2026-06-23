// Services/LoggingService.cs
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Models;
using System.Text;
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
                logEntry.IpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
                logEntry.RequestPath = httpContext.Request.Path;

                var userIdClaim = httpContext.User.FindFirst("UserId")?.Value
                               ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (Guid.TryParse(userIdClaim, out Guid userId))
                    logEntry.UserId = userId;

                var userNameClaim = httpContext.User.FindFirst("UserName")?.Value
                                 ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
                if (!string.IsNullOrEmpty(userNameClaim))
                    logEntry.UserName = userNameClaim;

                var roleClaim = httpContext.User.FindFirst("Role")?.Value
                             ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(roleClaim))
                    logEntry.Department = roleClaim;

                var wardNumberClaim = httpContext.User.FindFirst("WardNumber")?.Value;
                if (!string.IsNullOrEmpty(wardNumberClaim))
                    logEntry.WardNumber = wardNumberClaim;

                var emailClaim = httpContext.User.FindFirst("Email")?.Value
                              ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

                if (!string.IsNullOrEmpty(emailClaim))
                {
                    Dictionary<string, object> additionalData;

                    if (!string.IsNullOrEmpty(logEntry.AdditionalData))
                    {
                        try
                        {
                            additionalData = JsonSerializer.Deserialize<Dictionary<string, object>>(logEntry.AdditionalData)
                                             ?? new Dictionary<string, object>();
                        }
                        catch (JsonException)
                        {
                            additionalData = new Dictionary<string, object>();
                        }
                    }
                    else
                    {
                        additionalData = new Dictionary<string, object>();
                    }

                    additionalData["Email"] = emailClaim;
                    logEntry.AdditionalData = JsonSerializer.Serialize(additionalData);
                }
            }

            _context.Logs.Add(logEntry);
            await _context.SaveChangesAsync();
        }

        public async Task LogInfoAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Information,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });
        }

        public async Task LogWarningAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Warning,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });
        }

        public async Task LogErrorAsync(string message, Exception? ex = null, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Error,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ExceptionDetails = ex?.ToString(),
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });
        }

        public async Task LogDebugAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Debug,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });
        }

        public async Task LogCriticalAsync(string message, Exception? ex = null, LogCategory category = LogCategory.System, object? additionalData = null)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Critical,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                ExceptionDetails = ex?.ToString(),
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });
        }

        // ── Restore-capable audit logging ───────────────────────────────────
        public async Task LogChangeAsync<T>(
            string message,
            LogCategory category,
            string targetEntityType,
            Guid targetEntityId,
            T? before,
            T? after,
            object? additionalData = null)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Information,
                Category = category,
                Message = message,
                Timestamp = DateTime.UtcNow,
                TargetEntityType = targetEntityType,
                TargetEntityId = targetEntityId,
                BeforeState = before != null ? JsonSerializer.Serialize(before) : null,
                AfterState = after != null ? JsonSerializer.Serialize(after) : null,
                AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null
            });
        }

        // ── Citizen Services ──────────────────────────────────────────────────

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

        // ── Complaint/Grievance ───────────────────────────────────────────────

        public async Task LogComplaintAsync(Guid complaintId, string action, string status)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.Grievance,
                Message = $"Complaint #{complaintId}: {action}",
                ComplaintId = complaintId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { Action = action, Status = status })
            });
        }

        public async Task LogComplaintEscalationAsync(Guid complaintId, int fromLevel, int toLevel)
        {
            await LogWarningAsync(
                $"Complaint #{complaintId} escalated from level {fromLevel} to {toLevel}",
                LogCategory.Grievance,
                new { ComplaintId = complaintId, FromLevel = fromLevel, ToLevel = toLevel }
            );
        }

        // ── Payments ──────────────────────────────────────────────────────────

        public async Task LogPaymentAsync(int paymentId, string citizenId, decimal amount, string paymentType, string status)
        {
            await LogAsync(new LogEntry
            {
                Level = status == "Success" ? LogLevel.Information : LogLevel.Warning,
                Category = LogCategory.TaxCollection,
                Message = $"Payment {paymentId}: {paymentType} of ₹{amount:N2} - {status}",
                PaymentId = paymentId,
                CitizenId = citizenId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { Amount = amount, PaymentType = paymentType, Status = status })
            });
        }

        // ── Service Requests ─────────────────────────────────────────────────

        public async Task LogServiceRequestAsync(Guid serviceRequestId, string serviceType, string action, string status)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.ServiceRequests,
                Message = $"Service Request #{serviceRequestId} ({serviceType}): {action}",
                ServiceRequestId = serviceRequestId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { ServiceType = serviceType, Action = action, Status = status })
            });
        }

        // ── Waste Management ─────────────────────────────────────────────────

        public async Task LogWasteCollectionAsync(string routeId, string vehicleId, string action)
        {
            await LogInfoAsync(
                $"Waste Collection - {action}",
                LogCategory.WasteManagement,
                new { RouteId = routeId, VehicleId = vehicleId, Action = action }
            );
        }

        // ── Appointments & Queue ──────────────────────────────────────────────

        public async Task LogAppointmentAsync(Guid appointmentId, string citizenId, string action)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.Appointments,
                Message = $"Appointment #{appointmentId}: {action}",
                AppointmentId = appointmentId,
                CitizenId = citizenId,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task LogQueueActionAsync(int queueId, int tokenNumber, string action)
        {
            await LogInfoAsync(
                $"Queue #{queueId}: Token {tokenNumber} - {action}",
                LogCategory.Appointments,
                new { QueueId = queueId, TokenNumber = tokenNumber, Action = action }
            );
        }

        // ── Polls & Notices ───────────────────────────────────────────────────

        public async Task LogPollActionAsync(Guid pollId, string action)
        {
            await LogAsync(new LogEntry
            {
                Level = LogLevel.Information,
                Category = LogCategory.Polls,
                Message = $"Poll #{pollId}: {action}",
                PollId = pollId,
                Timestamp = DateTime.UtcNow
            });
        }

        public async Task LogNoticeActionAsync(Guid noticeId, string action)
        {
            await LogInfoAsync(
                $"Notice #{noticeId}: {action}",
                LogCategory.Notifications,
                new { NoticeId = noticeId, Action = action }
            );
        }

        // ── Document Verification ─────────────────────────────────────────────

        public async Task LogDocumentVerificationAsync(Guid documentId, string citizenId, bool isVerified, string? remarks = null)
        {
            var status = isVerified ? "Verified" : "Rejected";
            await LogAsync(new LogEntry
            {
                Level = isVerified ? LogLevel.Information : LogLevel.Warning,
                Category = LogCategory.DocumentVerification,
                Message = $"Document #{documentId} {status} for citizen {citizenId}",
                DocumentId = documentId,
                CitizenId = citizenId,
                Timestamp = DateTime.UtcNow,
                AdditionalData = JsonSerializer.Serialize(new { IsVerified = isVerified, Remarks = remarks })
            });
        }

        // ── User Management ───────────────────────────────────────────────────

        public async Task LogUserActionAsync(Guid userId, string action, string? details = null)
        {
            await LogInfoAsync(
                $"User {userId}: {action}",
                LogCategory.UserManagement,
                new { UserId = userId, Action = action, Details = details }
            );
        }

        // ── Query ─────────────────────────────────────────────────────────────

        public async Task<(IEnumerable<LogEntry> logs, int totalCount)> GetLogsAsync(LogQueryFilter filter)
        {
            filter.Validate();

            var query = _context.Logs
                .AsQueryable()
                .ApplyFilters(filter);

            var totalCount = await query.CountAsync();

            var logs = await query
                .ApplySorting(filter)
                .ApplyPagination(filter)
                .ToListAsync();

            return (logs, totalCount);
        }

        public async Task<LogEntry?> GetLogByIdAsync(int id)
        {
            return await _context.Logs.FindAsync(id);
        }

        public async Task<LogDashboardViewModel> GetDashboardStatsAsync()
        {
            var now = DateTime.UtcNow;
            var last24h = now.AddHours(-24);
            var todayStart = now.Date;
            var todayEnd = todayStart.AddDays(1);

            var totalLogsToday = await _context.Logs
                .CountAsync(l => l.Timestamp >= todayStart && l.Timestamp < todayEnd);

            var levelCounts = await _context.Logs
                .Where(l => l.Timestamp >= last24h)
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToListAsync();

            var categoryCounts = await _context.Logs
                .Where(l => l.Timestamp >= last24h)
                .GroupBy(l => l.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var recentErrors = await _context.Logs
                .Where(l => l.Timestamp >= last24h && l.Level == LogLevel.Error)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();

            var recentCitizenActions = await _context.Logs
                .Where(l => l.Timestamp >= last24h && l.Category == LogCategory.CitizenServices)
                .OrderByDescending(l => l.Timestamp)
                .Take(10)
                .ToListAsync();

            var topDepartments = await _context.Logs
                .Where(l => l.Timestamp >= last24h && l.Department != null)
                .GroupBy(l => l.Department!)
                .Select(g => new { Dept = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .ToListAsync();

            return new LogDashboardViewModel
            {
                TotalLogsToday = totalLogsToday,
                ErrorCount24h = levelCounts.FirstOrDefault(x => x.Level == LogLevel.Error)?.Count ?? 0,
                WarningCount24h = levelCounts.FirstOrDefault(x => x.Level == LogLevel.Warning)?.Count ?? 0,
                InfoCount24h = levelCounts.FirstOrDefault(x => x.Level == LogLevel.Information)?.Count ?? 0,
                CitizenServiceRequests = categoryCounts.FirstOrDefault(x => x.Category == LogCategory.CitizenServices)?.Count ?? 0,
                GrievancesFiled = await _context.Logs.CountAsync(l => l.Timestamp >= last24h && l.Category == LogCategory.Grievance && l.Message.Contains("filed")),
                GrievancesResolved = await _context.Logs.CountAsync(l => l.Timestamp >= last24h && l.Category == LogCategory.Grievance && l.Message.Contains("resolved")),
                LogsByCategory = categoryCounts.ToDictionary(g => g.Category, g => g.Count),
                RecentErrors = recentErrors,
                RecentCitizenActions = recentCitizenActions,
                TopActiveDepartments = topDepartments.ToDictionary(x => x.Dept, x => x.Count)
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
                    // Fall back to the generic TargetEntityType/TargetEntityId pair
                    // used by LogChangeAsync — this is what Staff and any future
                    // restore-enabled entity will hit.
                    query = query.Where(l =>
                        l.TargetEntityType != null &&
                        l.TargetEntityType.ToLower() == entityType.ToLower() &&
                        l.TargetEntityId == entityId);
                    break;
            }

            return await query
                .OrderByDescending(l => l.Timestamp)
                .Take(50)
                .ToListAsync();
        }

        // ── Export ────────────────────────────────────────────────────────────

        public async Task<byte[]> ExportLogsToCsvAsync(LogQueryFilter filter)
        {
            var logs = await GetAllLogsForExportAsync(filter);

            var sb = new StringBuilder();
            sb.AppendLine("Id,Timestamp,Level,Category,Message,UserName,WardNumber,Department,IpAddress,CorrelationId");

            foreach (var log in logs)
            {
                sb.AppendLine(string.Join(",",
                    log.Id,
                    log.Timestamp.ToString("o"),
                    log.Level,
                    log.Category,
                    $"\"{log.Message.Replace("\"", "\"\"")}\"",
                    log.UserName ?? "",
                    log.WardNumber ?? "",
                    log.Department ?? "",
                    log.IpAddress ?? "",
                    log.CorrelationId
                ));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportLogsToExcelAsync(LogQueryFilter filter)
        {
            return await ExportLogsToCsvAsync(filter);
        }

        public async Task<string> ExportLogsToJsonAsync(LogQueryFilter filter)
        {
            var logs = await GetAllLogsForExportAsync(filter);
            return JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
        }

        private async Task<List<LogEntry>> GetAllLogsForExportAsync(LogQueryFilter filter)
        {
            var query = _context.Logs
                .AsQueryable()
                .ApplyFilters(filter)
                .ApplySorting(filter);

            return await query.Take(100_000).ToListAsync();
        }

        // ── Statistics ────────────────────────────────────────────────────────

        public async Task<Dictionary<LogCategory, int>> GetCategoryWiseCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Logs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => l.Category)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Category, g => g.Count);
        }

        public async Task<Dictionary<LogLevel, int>> GetLevelWiseCountAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Logs
                .Where(l => l.Timestamp >= startDate && l.Timestamp <= endDate)
                .GroupBy(l => l.Level)
                .Select(g => new { Level = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Level, g => g.Count);
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

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