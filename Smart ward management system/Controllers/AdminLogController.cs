// Controllers/AdminLogController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Models;
using Smart_ward_management_system.Services;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,WardOfficer")]
    public class AdminLogController : ControllerBase
    {
        private readonly ILoggingService _logger;
        private readonly ApplicationDbContext _context;

        public AdminLogController(ILoggingService logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // GET: api/admin/AdminLog/logs
        [HttpGet("logs")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? level = null,
            [FromQuery] string? category = null,
            [FromQuery] string? searchTerm = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? wardNumber = null)
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    Level = !string.IsNullOrEmpty(level) ? Enum.Parse<LogLevel>(level) : null,
                    Category = !string.IsNullOrEmpty(category) ? Enum.Parse<LogCategory>(category) : null,
                    SearchTerm = searchTerm,
                    StartDate = startDate,
                    EndDate = endDate,
                    UserId = !string.IsNullOrEmpty(userId) ? Guid.Parse(userId) : null,
                    WardNumber = wardNumber
                };

                var (logs, totalCount) = await _logger.GetLogsAsync(filter);

                var response = new
                {
                    success = true,
                    data = logs.Select(l => new
                    {
                        l.Id,
                        l.Timestamp,
                        Level = l.Level.ToString(),
                        Category = l.Category.ToString(),
                        l.Message,
                        l.UserName,
                        l.UserId,
                        l.CitizenId,
                        l.WardNumber,
                        l.Department,
                        l.IpAddress,
                        l.RequestPath,
                        l.CorrelationId,
                        HasException = !string.IsNullOrEmpty(l.ExceptionDetails),
                        l.AdditionalData
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error fetching logs for admin", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error fetching logs" });
            }
        }

        // GET: api/admin/AdminLog/logs/{id}
        [HttpGet("logs/{id}")]
        public async Task<IActionResult> GetLogDetail(int id)
        {
            try
            {
                var log = await _logger.GetLogByIdAsync(id);

                if (log == null)
                {
                    return NotFound(new { success = false, message = "Log not found" });
                }

                // Get related logs with same correlation ID
                var filter = new LogQueryFilter
                {
                    SearchTerm = log.CorrelationId,
                    PageSize = 10
                };
                var (relatedLogs, _) = await _logger.GetLogsAsync(filter);

                var response = new
                {
                    success = true,
                    data = new
                    {
                        log.Id,
                        log.Timestamp,
                        Level = log.Level.ToString(),
                        Category = log.Category.ToString(),
                        log.Message,
                        log.UserName,
                        log.UserId,
                        log.CitizenId,
                        log.WardNumber,
                        log.Department,
                        log.IpAddress,
                        log.RequestPath,
                        log.CorrelationId,
                        log.ExceptionDetails,
                        AdditionalData = !string.IsNullOrEmpty(log.AdditionalData)
                            ? System.Text.Json.JsonSerializer.Deserialize<object>(log.AdditionalData)
                            : null,
                        relatedLogs = relatedLogs.Where(r => r.Id != log.Id).Select(r => new
                        {
                            r.Id,
                            r.Timestamp,
                            Level = r.Level.ToString(),
                            r.Message,
                            r.UserName
                        })
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching log detail for id {id}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error fetching log details" });
            }
        }

        // GET: api/admin/AdminLog/dashboard-stats
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _logger.GetDashboardStatsAsync();

                // Get additional stats for dashboard
                var today = DateTime.UtcNow.Date;
                var thisWeek = DateTime.UtcNow.AddDays(-7);

                var logsByDay = await _context.Logs
                    .Where(l => l.Timestamp >= thisWeek)
                    .GroupBy(l => l.Timestamp.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                var errorsByDay = await _context.Logs
                    .Where(l => l.Timestamp >= thisWeek && l.Level == LogLevel.Error)
                    .GroupBy(l => l.Timestamp.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                var response = new
                {
                    success = true,
                    data = new
                    {
                        totalLogsToday = stats.TotalLogsToday,
                        errorCount24h = stats.ErrorCount24h,
                        warningCount24h = stats.WarningCount24h,
                        citizenServiceRequests = stats.CitizenServiceRequests,
                        grievancesFiled = stats.GrievancesFiled,
                        grievancesResolved = stats.GrievancesResolved,
                        logsByCategory = stats.LogsByCategory.Select(kv => new { category = kv.Key.ToString(), count = kv.Value }),
                        recentErrors = stats.RecentErrors.Select(e => new
                        {
                            e.Id,
                            e.Timestamp,
                            e.Message,
                            e.UserName,
                            e.WardNumber
                        }),
                        recentCitizenActions = stats.RecentCitizenActions.Select(a => new
                        {
                            a.Id,
                            a.Timestamp,
                            a.Message,
                            a.CitizenId
                        }),
                        topActiveDepartments = stats.TopActiveDepartments.Select(kv => new { department = kv.Key, count = kv.Value }),
                        logsByDay = logsByDay,
                        errorsByDay = errorsByDay
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error fetching dashboard stats", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error fetching dashboard statistics" });
            }
        }

        // GET: api/admin/AdminLog/categories
        [HttpGet("categories")]
        public IActionResult GetLogCategories()
        {
            var categories = Enum.GetValues<LogCategory>()
                .Select(c => new { value = c.ToString(), label = c.ToString() })
                .ToList();

            return Ok(new { success = true, data = categories });
        }

        // GET: api/admin/AdminLog/levels
        [HttpGet("levels")]
        public IActionResult GetLogLevels()
        {
            var levels = Enum.GetValues<LogLevel>()
                .Select(l => new { value = l.ToString(), label = l.ToString() })
                .ToList();

            return Ok(new { success = true, data = levels });
        }

        // GET: api/admin/AdminLog/export
        [HttpGet("export")]
        public async Task<IActionResult> ExportLogs(
            [FromQuery] string? level = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? format = "csv")
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    Level = !string.IsNullOrEmpty(level) ? Enum.Parse<LogLevel>(level) : null,
                    Category = !string.IsNullOrEmpty(category) ? Enum.Parse<LogCategory>(category) : null,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageSize = 50000 // Export max 50000 records
                };

                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format.ToLower())
                {
                    case "csv":
                        fileBytes = await _logger.ExportLogsToCsvAsync(filter);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        contentType = "text/csv";
                        break;
                    case "json":
                        var json = await _logger.ExportLogsToJsonAsync(filter);
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        contentType = "application/json";
                        break;
                    default:
                        return BadRequest(new { success = false, message = "Unsupported format" });
                }

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error exporting logs", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error exporting logs" });
            }
        }

        // GET: api/admin/AdminLog/audit/user/{userId}
        [HttpGet("audit/user/{userId}")]
        public async Task<IActionResult> GetUserAuditTrail(Guid userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                var filter = new LogQueryFilter
                {
                    UserId = userId,
                    PageNumber = page,
                    PageSize = pageSize,
                    SortDescending = true
                };

                var (logs, totalCount) = await _logger.GetLogsAsync(filter);

                var response = new
                {
                    success = true,
                    data = logs.Select(l => new
                    {
                        l.Id,
                        l.Timestamp,
                        Level = l.Level.ToString(),
                        Category = l.Category.ToString(),
                        l.Message,
                        l.IpAddress,
                        l.RequestPath
                    }),
                    pagination = new
                    {
                        currentPage = page,
                        pageSize = pageSize,
                        totalCount = totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching user audit trail for {userId}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error fetching audit trail" });
            }
        }

        // GET: api/admin/AdminLog/audit/entity/{entityType}/{entityId}
        [HttpGet("audit/entity/{entityType}/{entityId}")]
        public async Task<IActionResult> GetEntityAuditTrail(string entityType, Guid entityId)
        {
            try
            {
                var logs = await _logger.GetEntityLogsAsync(entityType, entityId);

                var response = new
                {
                    success = true,
                    entityType = entityType,
                    entityId = entityId,
                    data = logs.Select(l => new
                    {
                        l.Id,
                        l.Timestamp,
                        Level = l.Level.ToString(),
                        l.Message,
                        l.UserName,
                        l.AdditionalData
                    })
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching entity audit trail for {entityType}/{entityId}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error fetching audit trail" });
            }
        }

        // GET: api/admin/AdminLog/statistics
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var end = endDate ?? DateTime.UtcNow;
                var start = startDate ?? end.AddDays(-30);

                var categoryStats = await _logger.GetCategoryWiseCountAsync(start, end);
                var levelStats = await _logger.GetLevelWiseCountAsync(start, end);

                // Get daily trends
                var dailyTrends = await _context.Logs
                    .Where(l => l.Timestamp >= start && l.Timestamp <= end)
                    .GroupBy(l => l.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Total = g.Count(),
                        Errors = g.Count(l => l.Level == LogLevel.Error),
                        Warnings = g.Count(l => l.Level == LogLevel.Warning)
                    })
                    .OrderBy(g => g.Date)
                    .ToListAsync();

                // Get top users by activity
                var topUsers = await _context.Logs
                    .Where(l => l.Timestamp >= start && l.Timestamp <= end && l.UserId.HasValue)
                    .GroupBy(l => new { l.UserId, l.UserName })
                    .Select(g => new
                    {
                        UserId = g.Key.UserId,
                        UserName = g.Key.UserName,
                        ActivityCount = g.Count()
                    })
                    .OrderByDescending(g => g.ActivityCount)
                    .Take(10)
                    .ToListAsync();

                var response = new
                {
                    success = true,
                    data = new
                    {
                        period = new { startDate = start, endDate = end },
                        categoryStats = categoryStats.Select(kv => new { category = kv.Key.ToString(), count = kv.Value }),
                        levelStats = levelStats.Select(kv => new { level = kv.Key.ToString(), count = kv.Value }),
                        dailyTrends = dailyTrends,
                        topUsers = topUsers,
                        summary = new
                        {
                            totalLogs = await _context.Logs.CountAsync(l => l.Timestamp >= start && l.Timestamp <= end),
                            totalErrors = levelStats.GetValueOrDefault(LogLevel.Error),
                            uniqueUsers = topUsers.Count,
                            averageDailyLogs = dailyTrends.Any() ? dailyTrends.Average(d => d.Total) : 0
                        }
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error fetching statistics", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error fetching statistics" });
            }
        }

        // DELETE: api/admin/AdminLog/clear-old
        [HttpDelete("clear-old")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var deletedCount = await _logger.ClearOldLogsAsync(cutoffDate);

                await _logger.LogInfoAsync($"Admin cleared {deletedCount} old logs (older than {daysToKeep} days)",
                    LogCategory.Audit,
                    new { DaysToKeep = daysToKeep, DeletedCount = deletedCount });

                return Ok(new
                {
                    success = true,
                    message = $"Successfully cleared {deletedCount} logs older than {daysToKeep} days",
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error clearing old logs", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error clearing old logs" });
            }
        }
    }
}