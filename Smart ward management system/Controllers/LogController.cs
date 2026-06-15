// Controllers/LogController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.ViewModels;
using Smart_ward_management_system.Models;
using Smart_ward_management_system.Services;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Controllers
{
    // ✅ FIX Bug 6: Changed from Controller (MVC) to ControllerBase (API).
    // Angular expects JSON responses, not Razor views or redirects.
    // Added [ApiController] for automatic model validation and JSON binding.
    // Added [Route] so Angular's LogService can call predictable endpoints.
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin,WardOfficer,Supervisor")]
    public class LogController : ControllerBase
    {
        private readonly ILoggingService _loggingService;

        public LogController(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        // GET: api/log/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var stats = await _loggingService.GetDashboardStatsAsync();
                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error loading dashboard", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading dashboard statistics" });
            }
        }

        // GET: api/log?page=1&pageSize=20&level=...&category=...
        [HttpGet]
        public async Task<IActionResult> Index(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] LogLevel? level = null,
            [FromQuery] LogCategory? category = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? sortBy = "Timestamp",
            [FromQuery] bool sortDescending = true)
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    Level = level,
                    Category = category,
                    SearchTerm = searchTerm,
                    StartDate = startDate,
                    EndDate = endDate,
                    SortBy = sortBy,
                    SortDescending = sortDescending
                };

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error loading logs", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading logs" });
            }
        }

        // GET: api/log/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var log = await _loggingService.GetLogByIdAsync(id);
                if (log == null)
                {
                    await _loggingService.LogWarningAsync($"Log with ID {id} not found", LogCategory.System);
                    return NotFound(new { success = false, message = $"Log {id} not found" });
                }

                // ✅ FIX Bug 8: Wrap AdditionalData deserialization in try/catch
                // so a malformed JSON field doesn't crash the entire endpoint.
                Dictionary<string, object>? parsedAdditionalData = null;
                if (!string.IsNullOrEmpty(log.AdditionalData))
                {
                    try
                    {
                        parsedAdditionalData = System.Text.Json.JsonSerializer
                            .Deserialize<Dictionary<string, object>>(log.AdditionalData);
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Malformed JSON — return raw string rather than throwing
                        parsedAdditionalData = new Dictionary<string, object>
                        {
                            ["_raw"] = log.AdditionalData
                        };
                    }
                }

                // Get related logs by correlation ID
                List<LogEntry> relatedLogs = new();
                if (!string.IsNullOrEmpty(log.CorrelationId))
                {
                    var (related, _) = await _loggingService.GetLogsAsync(new LogQueryFilter
                    {
                        SearchTerm = log.CorrelationId,
                        PageSize = 10
                    });
                    relatedLogs = related.Where(l => l.Id != id).ToList();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        log,
                        parsedAdditionalData,
                        relatedLogs
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading log details for ID {id}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading log details" });
            }
        }

        // GET: api/log/export?format=csv&level=...
        [HttpGet("export")]
        public async Task<IActionResult> Export(
            [FromQuery] LogLevel? level,
            [FromQuery] LogCategory? category,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string format = "csv")
        {
            try
            {
                // ✅ FIX Bug 7: Do NOT call filter.Validate() for exports.
                // LoggingService.GetAllLogsForExportAsync bypasses the 100-row cap.
                var filter = new LogQueryFilter
                {
                    Level = level,
                    Category = category,
                    StartDate = startDate,
                    EndDate = endDate,
                    ExportAll = true
                };

                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format.ToLower())
                {
                    case "csv":
                        fileBytes = await _loggingService.ExportLogsToCsvAsync(filter);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        contentType = "text/csv";
                        break;

                    case "excel":
                        fileBytes = await _loggingService.ExportLogsToExcelAsync(filter);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                        contentType = "application/vnd.ms-excel";
                        break;

                    case "json":
                        var json = await _loggingService.ExportLogsToJsonAsync(filter);
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        contentType = "application/json";
                        break;

                    default:
                        return BadRequest(new { success = false, message = "Invalid format. Supported: csv, excel, json" });
                }

                await _loggingService.LogInfoAsync(
                    $"Exported logs to {format.ToUpper()}",
                    LogCategory.System,
                    new { Format = format, Filter = filter.GetFilterSummary() });

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error exporting logs", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error exporting logs" });
            }
        }

        // GET: api/log/citizen-activity?citizenId=...&page=1
        [HttpGet("citizen-activity")]
        public async Task<IActionResult> CitizenActivity([FromQuery] string citizenId, [FromQuery] int page = 1)
        {
            if (string.IsNullOrEmpty(citizenId))
                return BadRequest(new { success = false, message = "Citizen ID required" });

            try
            {
                var filter = new LogQueryFilter
                {
                    CitizenId = citizenId,
                    PageNumber = page,
                    PageSize = 20
                };

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        citizenId,
                        currentPage = page,
                        totalPages = (int)Math.Ceiling(totalCount / 20.0),
                        totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading citizen activity for {citizenId}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading citizen activity" });
            }
        }

        // GET: api/log/complaint-activity?complaintId=...&page=1
        [HttpGet("complaint-activity")]
        public async Task<IActionResult> ComplaintActivity([FromQuery] Guid complaintId, [FromQuery] int page = 1)
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    ComplaintId = complaintId,
                    PageNumber = page,
                    PageSize = 20
                };

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        complaintId,
                        currentPage = page,
                        totalPages = (int)Math.Ceiling(totalCount / 20.0),
                        totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading complaint activity for {complaintId}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading complaint activity" });
            }
        }

        // GET: api/log/service-request-activity?serviceRequestId=...&page=1
        [HttpGet("service-request-activity")]
        public async Task<IActionResult> ServiceRequestActivity([FromQuery] Guid serviceRequestId, [FromQuery] int page = 1)
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    ServiceRequestId = serviceRequestId,
                    PageNumber = page,
                    PageSize = 20
                };

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        serviceRequestId,
                        currentPage = page,
                        totalPages = (int)Math.Ceiling(totalCount / 20.0),
                        totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading service request activity for {serviceRequestId}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading service request activity" });
            }
        }

        // GET: api/log/statistics?startDate=...&endDate=...
        [HttpGet("statistics")]
        public async Task<IActionResult> Statistics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);

                var (_, totalCount) = await _loggingService.GetLogsAsync(new LogQueryFilter { StartDate = start, EndDate = end, PageSize = 1 });
                var categoryStats = await _loggingService.GetCategoryWiseCountAsync(start, end);
                var levelStats = await _loggingService.GetLevelWiseCountAsync(start, end);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        startDate = start,
                        endDate = end,
                        totalLogs = totalCount,
                        categoryStats,
                        levelStats
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error loading statistics", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading statistics" });
            }
        }

        // POST: api/log/clear-old-logs
        [HttpPost("clear-old-logs")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var deletedCount = await _loggingService.ClearOldLogsAsync(cutoffDate);

                await _loggingService.LogInfoAsync(
                    $"Cleared {deletedCount} logs older than {daysToKeep} days",
                    LogCategory.System,
                    new { DaysToKeep = daysToKeep, DeletedCount = deletedCount });

                return Ok(new
                {
                    success = true,
                    message = $"Successfully cleared {deletedCount} logs older than {daysToKeep} days.",
                    deletedCount
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error clearing old logs", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error clearing old logs" });
            }
        }

        // GET: api/log/audit-trail?entityType=complaint&entityId=...&page=1
        [HttpGet("audit-trail")]
        public async Task<IActionResult> AuditTrail(
            [FromQuery] string entityType,
            [FromQuery] Guid entityId,
            [FromQuery] int page = 1)
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    PageNumber = page,
                    PageSize = 20,
                    SearchTerm = entityId.ToString()
                };

                if (entityType.ToLower() == "complaint")
                    filter.ComplaintId = entityId;
                else if (entityType.ToLower() == "servicerequest")
                    filter.ServiceRequestId = entityId;

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        entityType,
                        entityId,
                        currentPage = page,
                        totalPages = (int)Math.Ceiling(totalCount / 20.0),
                        totalCount
                    }
                });
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading audit trail for {entityType}/{entityId}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error loading audit trail" });
            }
        }

        // GET: api/log/levels  — used by Angular dropdown
        [HttpGet("levels")]
        public IActionResult GetLogLevels()
        {
            var levels = Enum.GetValues<LogLevel>()
                .Select(l => new { key = l.ToString(), value = l.ToString(), numericValue = (int)l })
                .ToList();
            return Ok(new { success = true, data = levels });
        }

        // GET: api/log/categories  — used by Angular dropdown
        [HttpGet("categories")]
        public IActionResult GetLogCategories()
        {
            var categories = Enum.GetValues<LogCategory>()
                .Select(c => new { key = c.ToString(), value = c.ToString(), numericValue = (int)c })
                .ToList();
            return Ok(new { success = true, data = categories });
        }
    }
}