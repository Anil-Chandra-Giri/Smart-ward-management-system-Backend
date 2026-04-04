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
    [Authorize(Roles = "Admin,WardOfficer,Supervisor")]
    public class LogController : Controller
    {
        private readonly ILoggingService _loggingService;

        public LogController(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        // GET: Log/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var stats = await _loggingService.GetDashboardStatsAsync();
                return View(stats);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error loading dashboard", ex, LogCategory.System);
                TempData["Error"] = "Error loading dashboard statistics";
                return View(new LogDashboardViewModel());
            }
        }

        // GET: Log/Index
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20,
            LogLevel? level = null, LogCategory? category = null,
            string? searchTerm = null, DateTime? startDate = null, DateTime? endDate = null)
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
                    EndDate = endDate
                };

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                ViewBag.PageSize = pageSize;
                ViewBag.CurrentLevel = level;
                ViewBag.CurrentCategory = category;
                ViewBag.CurrentSearchTerm = searchTerm;
                ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
                ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
                ViewBag.TotalCount = totalCount;

                return View(logs);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error loading logs index", ex, LogCategory.System);
                TempData["Error"] = "Error loading logs";
                return View(new List<LogEntry>());
            }
        }

        // GET: Log/Details/5
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var log = await _loggingService.GetLogByIdAsync(id);
                if (log == null)
                {
                    await _loggingService.LogWarningAsync($"Log with ID {id} not found", LogCategory.System);
                    return NotFound();
                }

                var viewModel = new LogDetailsViewModel
                {
                    Log = log,
                    ParsedAdditionalData = !string.IsNullOrEmpty(log.AdditionalData)
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(log.AdditionalData)
                        : null
                };

                // Get related logs (same correlation ID)
                if (!string.IsNullOrEmpty(log.CorrelationId))
                {
                    var (relatedLogs, _) = await _loggingService.GetLogsAsync(new LogQueryFilter
                    {
                        SearchTerm = log.CorrelationId,
                        PageSize = 10
                    });
                    viewModel.RelatedLogs = relatedLogs.Where(l => l.Id != id).ToList();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading log details for ID {id}", ex, LogCategory.System);
                TempData["Error"] = "Error loading log details";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Log/Export
        public async Task<IActionResult> Export(LogLevel? level, LogCategory? category,
            DateTime? startDate, DateTime? endDate, string format = "csv")
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    Level = level,
                    Category = category,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageSize = 100000 // Export all (max 100k records)
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
                        return BadRequest(new { message = "Invalid format. Supported formats: csv, excel, json" });
                }

                await _loggingService.LogInfoAsync($"Exported {filter.PageSize} logs to {format.ToUpper()} format",
                    LogCategory.System,
                    new { Format = format, Filter = filter.GetFilterSummary() });

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error exporting logs", ex, LogCategory.System);
                TempData["Error"] = "Error exporting logs";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Log/CitizenActivity
        public async Task<IActionResult> CitizenActivity(string citizenId, int page = 1)
        {
            if (string.IsNullOrEmpty(citizenId))
                return BadRequest("Citizen ID required");

            try
            {
                var filter = new LogQueryFilter
                {
                    CitizenId = citizenId,
                    PageNumber = page,
                    PageSize = 20
                };

                var (logs, totalCount) = await _loggingService.GetLogsAsync(filter);

                ViewBag.CitizenId = citizenId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 20.0);
                ViewBag.TotalCount = totalCount;

                return View(logs);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading citizen activity for {citizenId}", ex, LogCategory.System);
                TempData["Error"] = "Error loading citizen activity";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Log/ComplaintActivity
        public async Task<IActionResult> ComplaintActivity(Guid complaintId, int page = 1)
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

                ViewBag.ComplaintId = complaintId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 20.0);

                return View(logs);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading complaint activity for {complaintId}", ex, LogCategory.System);
                TempData["Error"] = "Error loading complaint activity";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Log/ServiceRequestActivity
        public async Task<IActionResult> ServiceRequestActivity(Guid serviceRequestId, int page = 1)
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

                ViewBag.ServiceRequestId = serviceRequestId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 20.0);

                return View(logs);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading service request activity for {serviceRequestId}", ex, LogCategory.System);
                TempData["Error"] = "Error loading service request activity";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Log/Statistics
        public async Task<IActionResult> Statistics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);

                var filter = new LogQueryFilter
                {
                    StartDate = start,
                    EndDate = end,
                    PageSize = 1
                };

                var (_, totalCount) = await _loggingService.GetLogsAsync(filter);
                var categoryStats = await _loggingService.GetCategoryWiseCountAsync(start, end);
                var levelStats = await _loggingService.GetLevelWiseCountAsync(start, end);

                ViewBag.StartDate = start;
                ViewBag.EndDate = end;
                ViewBag.TotalLogs = totalCount;
                ViewBag.CategoryStats = categoryStats;
                ViewBag.LevelStats = levelStats;

                return View();
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync("Error loading statistics", ex, LogCategory.System);
                TempData["Error"] = "Error loading statistics";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        // POST: Log/ClearOldLogs
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearOldLogs(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var oldLogsCount = await _loggingService.ClearOldLogsAsync(cutoffDate);

                await _loggingService.LogInfoAsync($"Cleared {oldLogsCount} logs older than {daysToKeep} days",
                    LogCategory.System,
                    new { DaysToKeep = daysToKeep, DeletedCount = oldLogsCount });

                TempData["Success"] = $"Successfully cleared {oldLogsCount} logs older than {daysToKeep} days.";
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error clearing old logs", ex, LogCategory.System);
                TempData["Error"] = "Error clearing old logs";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Log/AuditTrail
        public async Task<IActionResult> AuditTrail(string entityType, Guid entityId, int page = 1)
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

                ViewBag.EntityType = entityType;
                ViewBag.EntityId = entityId;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = (int)Math.Ceiling(totalCount / 20.0);

                return View(logs);
            }
            catch (Exception ex)
            {
                await _loggingService.LogErrorAsync($"Error loading audit trail for {entityType}/{entityId}", ex, LogCategory.System);
                TempData["Error"] = "Error loading audit trail";
                return RedirectToAction(nameof(Index));
            }
        }
    }
}