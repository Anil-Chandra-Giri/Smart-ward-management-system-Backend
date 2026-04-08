// Services/ILoggingService.cs
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Models;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Services
{
    public interface ILoggingService
    {
        // Core logging methods
        Task LogAsync(LogEntry logEntry);
        Task LogDebugAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null);
        Task LogInfoAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null);
        Task LogWarningAsync(string message, LogCategory category = LogCategory.System, object? additionalData = null);
        Task LogErrorAsync(string message, Exception? ex = null, LogCategory category = LogCategory.System, object? additionalData = null);
        Task LogCriticalAsync(string message, Exception? ex = null, LogCategory category = LogCategory.System, object? additionalData = null);

        // Citizen Services
        Task LogCitizenActionAsync(string citizenId, string action, string? department = null);
        Task LogCertificateRequestAsync(string requestType, string citizenId, string requestId, string action);

        // Complaint/Grievance
        Task LogComplaintAsync(Guid complaintId, string action, string status);
        Task LogComplaintEscalationAsync(Guid complaintId, int fromLevel, int toLevel);

        // Payments
        Task LogPaymentAsync(int paymentId, string citizenId, decimal amount, string paymentType, string status);

        // Service Requests
        Task LogServiceRequestAsync(Guid serviceRequestId, string serviceType, string action, string status);

        // Waste Management
        Task LogWasteCollectionAsync(string routeId, string vehicleId, string action);

        // Appointments & Queue
        Task LogAppointmentAsync(Guid appointmentId, string citizenId, string action);
        Task LogQueueActionAsync(int queueId, int tokenNumber, string action);

        // Polls & Notices
        Task LogPollActionAsync(Guid pollId, string action);
        Task LogNoticeActionAsync(Guid noticeId, string action);

        // Document Verification
        Task LogDocumentVerificationAsync(Guid documentId, string citizenId, bool isVerified, string? remarks = null);

        // User Management
        Task LogUserActionAsync(Guid userId, string action, string? details = null);

        // Query methods
        Task<(IEnumerable<LogEntry> logs, int totalCount)> GetLogsAsync(LogQueryFilter filter);
        Task<LogEntry?> GetLogByIdAsync(int id);
        Task<LogDashboardViewModel> GetDashboardStatsAsync();
        Task<IEnumerable<LogEntry>> GetEntityLogsAsync(string entityType, Guid entityId);

        // Export methods - ADD THESE
        Task<byte[]> ExportLogsToCsvAsync(LogQueryFilter filter);
        Task<byte[]> ExportLogsToExcelAsync(LogQueryFilter filter);
        Task<string> ExportLogsToJsonAsync(LogQueryFilter filter);

        // Add these to ILoggingService.cs

        // Statistics methods
        Task<Dictionary<LogCategory, int>> GetCategoryWiseCountAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<LogLevel, int>> GetLevelWiseCountAsync(DateTime startDate, DateTime endDate);

        // Cleanup method
        Task<int> ClearOldLogsAsync(DateTime cutoffDate);
    }
}