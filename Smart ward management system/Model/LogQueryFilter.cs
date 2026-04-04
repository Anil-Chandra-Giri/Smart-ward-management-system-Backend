// Models/LogQueryFilter.cs
using Smart_ward_management_system.Model;
using System;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Models
{
    public class LogQueryFilter
    {
        // Pagination properties
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        // Date range filters
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Log level and category filters
        public LogLevel? Level { get; set; }
        public LogCategory? Category { get; set; }

        // User and citizen filters
        public Guid? UserId { get; set; }
        public string? CitizenId { get; set; }
        public string? UserName { get; set; }

        // Location filters
        public string? WardNumber { get; set; }
        public string? Department { get; set; }
        public string? Municipality { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }

        // Entity-specific filters (Foreign Keys)
        public Guid? ComplaintId { get; set; }
        public Guid? ServiceRequestId { get; set; }
        public int? PaymentId { get; set; }
        public Guid? DocumentId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid? PollId { get; set; }
        public int? QueueId { get; set; }

        // Complaint-specific filters
        public string? ComplaintStatus { get; set; }
        public string? ComplaintPriority { get; set; }
        public string? ComplaintCategory { get; set; }

        // Service Request specific filters
        public string? ServiceRequestStatus { get; set; }
        public string? ServiceType { get; set; }
        public string? ApplicationNumber { get; set; }

        // Payment specific filters
        public string? PaymentStatus { get; set; }
        public string? PaymentType { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }

        // Search and filtering
        public string? SearchTerm { get; set; }
        public string? GrievanceId { get; set; }
        public string? ApplicationId { get; set; }

        // Sorting
        public string? SortBy { get; set; } = "Timestamp";
        public bool SortDescending { get; set; } = true;

        // Export options
        public bool ExportAll { get; set; } = false;
        public string? ExportFormat { get; set; } = "csv";

        // Additional filters
        public bool? HasException { get; set; }
        public string? IpAddress { get; set; }
        public string? CorrelationId { get; set; }

        // Helper method to check if any filter is applied
        public bool HasFilters()
        {
            return StartDate.HasValue ||
                   EndDate.HasValue ||
                   Level.HasValue ||
                   Category.HasValue ||
                   UserId.HasValue ||
                   !string.IsNullOrEmpty(CitizenId) ||
                   !string.IsNullOrEmpty(WardNumber) ||
                   !string.IsNullOrEmpty(Department) ||
                   ComplaintId.HasValue ||
                   ServiceRequestId.HasValue ||
                   PaymentId.HasValue ||
                   !string.IsNullOrEmpty(SearchTerm) ||
                   !string.IsNullOrEmpty(ComplaintStatus) ||
                   !string.IsNullOrEmpty(ServiceRequestStatus) ||
                   HasException.HasValue;
        }

        // Helper method to get skip count for pagination
        public int GetSkipCount()
        {
            return (PageNumber - 1) * PageSize;
        }

        // Helper method to validate page number
        public void Validate()
        {
            if (PageNumber < 1) PageNumber = 1;
            if (PageSize < 1) PageSize = 20;
            if (PageSize > 100) PageSize = 100; // Max page size limit
        }

        // Helper method to get date range as string for logging
        public string GetDateRangeString()
        {
            if (StartDate.HasValue && EndDate.HasValue)
                return $"{StartDate.Value:yyyy-MM-dd} to {EndDate.Value:yyyy-MM-dd}";
            if (StartDate.HasValue)
                return $"From {StartDate.Value:yyyy-MM-dd}";
            if (EndDate.HasValue)
                return $"Until {EndDate.Value:yyyy-MM-dd}";
            return "All time";
        }

        // Helper method to get filter summary for logging
        public string GetFilterSummary()
        {
            var filters = new List<string>();

            if (Level.HasValue) filters.Add($"Level={Level.Value}");
            if (Category.HasValue) filters.Add($"Category={Category.Value}");
            if (UserId.HasValue) filters.Add($"UserId={UserId.Value}");
            if (!string.IsNullOrEmpty(CitizenId)) filters.Add($"CitizenId={CitizenId}");
            if (!string.IsNullOrEmpty(WardNumber)) filters.Add($"Ward={WardNumber}");
            if (!string.IsNullOrEmpty(Department)) filters.Add($"Dept={Department}");
            if (ComplaintId.HasValue) filters.Add($"ComplaintId={ComplaintId.Value}");
            if (ServiceRequestId.HasValue) filters.Add($"ServiceRequestId={ServiceRequestId.Value}");
            if (!string.IsNullOrEmpty(SearchTerm)) filters.Add($"Search='{SearchTerm}'");
            if (!string.IsNullOrEmpty(ComplaintStatus)) filters.Add($"ComplaintStatus={ComplaintStatus}");

            return filters.Count > 0 ? string.Join(", ", filters) : "No filters";
        }
    }

    // Extension methods for LogQueryFilter
    public static class LogQueryFilterExtensions
    {
        public static IQueryable<LogEntry> ApplyFilters(this IQueryable<LogEntry> query, LogQueryFilter filter)
        {
            if (filter == null) return query;

            // Date range filters
            if (filter.StartDate.HasValue)
                query = query.Where(l => l.Timestamp >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(l => l.Timestamp <= filter.EndDate.Value);

            // Level and category filters
            if (filter.Level.HasValue)
                query = query.Where(l => l.Level == filter.Level.Value);
            if (filter.Category.HasValue)
                query = query.Where(l => l.Category == filter.Category.Value);

            // User and citizen filters
            if (filter.UserId.HasValue)
                query = query.Where(l => l.UserId == filter.UserId.Value);
            if (!string.IsNullOrEmpty(filter.CitizenId))
                query = query.Where(l => l.CitizenId == filter.CitizenId);

            // Location filters
            if (!string.IsNullOrEmpty(filter.WardNumber))
                query = query.Where(l => l.WardNumber == filter.WardNumber);
            if (!string.IsNullOrEmpty(filter.Department))
                query = query.Where(l => l.Department == filter.Department);

            // Entity-specific filters
            if (filter.ComplaintId.HasValue)
                query = query.Where(l => l.ComplaintId == filter.ComplaintId.Value);
            if (filter.ServiceRequestId.HasValue)
                query = query.Where(l => l.ServiceRequestId == filter.ServiceRequestId.Value);
            if (filter.PaymentId.HasValue)
                query = query.Where(l => l.PaymentId == filter.PaymentId.Value);
            if (filter.DocumentId.HasValue)
                query = query.Where(l => l.DocumentId == filter.DocumentId.Value);
            if (filter.AppointmentId.HasValue)
                query = query.Where(l => l.AppointmentId == filter.AppointmentId.Value);
            if (filter.PollId.HasValue)
                query = query.Where(l => l.PollId == filter.PollId.Value);

            // Search term
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(l =>
                    l.Message.Contains(filter.SearchTerm) ||
                    (l.AdditionalData != null && l.AdditionalData.Contains(filter.SearchTerm)) ||
                    (l.CitizenId != null && l.CitizenId.Contains(filter.SearchTerm)) ||
                    (l.CorrelationId != null && l.CorrelationId.Contains(filter.SearchTerm)));
            }

            // Exception filter
            if (filter.HasException.HasValue && filter.HasException.Value)
                query = query.Where(l => l.ExceptionDetails != null);
            else if (filter.HasException.HasValue && !filter.HasException.Value)
                query = query.Where(l => l.ExceptionDetails == null);

            // IP Address filter
            if (!string.IsNullOrEmpty(filter.IpAddress))
                query = query.Where(l => l.IpAddress == filter.IpAddress);

            // Correlation ID filter
            if (!string.IsNullOrEmpty(filter.CorrelationId))
                query = query.Where(l => l.CorrelationId == filter.CorrelationId);

            return query;
        }

        public static IQueryable<LogEntry> ApplySorting(this IQueryable<LogEntry> query, LogQueryFilter filter)
        {
            if (string.IsNullOrEmpty(filter.SortBy))
                filter.SortBy = "Timestamp";

            switch (filter.SortBy.ToLower())
            {
                case "timestamp":
                    query = filter.SortDescending
                        ? query.OrderByDescending(l => l.Timestamp)
                        : query.OrderBy(l => l.Timestamp);
                    break;
                case "level":
                    query = filter.SortDescending
                        ? query.OrderByDescending(l => l.Level)
                        : query.OrderBy(l => l.Level);
                    break;
                case "category":
                    query = filter.SortDescending
                        ? query.OrderByDescending(l => l.Category)
                        : query.OrderBy(l => l.Category);
                    break;
                case "wardnumber":
                    query = filter.SortDescending
                        ? query.OrderByDescending(l => l.WardNumber)
                        : query.OrderBy(l => l.WardNumber);
                    break;
                default:
                    query = filter.SortDescending
                        ? query.OrderByDescending(l => l.Timestamp)
                        : query.OrderBy(l => l.Timestamp);
                    break;
            }

            return query;
        }

        public static IQueryable<LogEntry> ApplyPagination(this IQueryable<LogEntry> query, LogQueryFilter filter)
        {
            return query.Skip(filter.GetSkipCount()).Take(filter.PageSize);
        }
    }
}