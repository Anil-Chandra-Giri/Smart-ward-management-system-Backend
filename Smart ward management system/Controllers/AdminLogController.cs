// Controllers/AdminLogController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Models;
using Smart_ward_management_system.Services;
using Smart_ward_management_system.Services.Restore;
using LogLevel = Smart_ward_management_system.Model.LogLevel;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,WardOfficer")]
    public class AdminLogController : ControllerBase
    {
        private readonly ILoggingService _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEntityRestoreDispatcher _restoreDispatcher;

        public AdminLogController(ILoggingService logger, ApplicationDbContext context, IEntityRestoreDispatcher restoreDispatcher)
        {
            _logger = logger;
            _context = context;
            _restoreDispatcher = restoreDispatcher;
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
                // FIX: Use ignoreCase: true on all Enum.Parse calls so that
                // "information", "Information", "INFORMATION" all parse correctly.
                // Previously, a casing mismatch threw an ArgumentException → 500
                // → response.success was never true → Angular showed an empty grid.
                LogLevel? parsedLevel = null;
                if (!string.IsNullOrEmpty(level))
                {
                    if (!Enum.TryParse<LogLevel>(level, ignoreCase: true, out var lv))
                        return BadRequest(new { success = false, message = $"Invalid log level: '{level}'" });
                    parsedLevel = lv;
                }

                LogCategory? parsedCategory = null;
                if (!string.IsNullOrEmpty(category))
                {
                    if (!Enum.TryParse<LogCategory>(category, ignoreCase: true, out var cat))
                        return BadRequest(new { success = false, message = $"Invalid category: '{category}'" });
                    parsedCategory = cat;
                }

                var filter = new LogQueryFilter
                {
                    PageNumber = page,
                    PageSize = pageSize,
                    Level = parsedLevel,
                    Category = parsedCategory,
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
                        Level = l.Level.ToString(),       // always string to frontend
                        Category = l.Category.ToString(), // always string to frontend
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
                        l.AdditionalData,
                        // Whether this entry has a snapshot a restore can act on
                        IsRestorable = !string.IsNullOrEmpty(l.TargetEntityType)
                                       && l.TargetEntityId != null
                                       && !string.IsNullOrEmpty(l.BeforeState),
                        l.TargetEntityType,
                        l.TargetEntityId
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
                    return NotFound(new { success = false, message = "Log not found" });

                var filter = new LogQueryFilter { SearchTerm = log.CorrelationId, PageSize = 10 };
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
                        IsRestorable = !string.IsNullOrEmpty(log.TargetEntityType)
                                       && log.TargetEntityId != null
                                       && !string.IsNullOrEmpty(log.BeforeState),
                        log.TargetEntityType,
                        log.TargetEntityId,
                        BeforeState = !string.IsNullOrEmpty(log.BeforeState)
                            ? System.Text.Json.JsonSerializer.Deserialize<object>(log.BeforeState)
                            : null,
                        AfterState = !string.IsNullOrEmpty(log.AfterState)
                            ? System.Text.Json.JsonSerializer.Deserialize<object>(log.AfterState)
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

        // POST: api/admin/AdminLog/logs/{id}/restore
        [HttpPost("logs/{id}/restore")]
        public async Task<IActionResult> RestoreFromLog(int id)
        {
            try
            {
                var result = await _restoreDispatcher.RestoreFromLogAsync(id);

                return Ok(new
                {
                    success = true,
                    message = result.Recreated
                        ? "Record restored. It had been deleted, so a new temporary password was generated since the original isn't recoverable."
                        : "Record restored to its previous state.",
                    recreated = result.Recreated,
                    newUsername = result.NewUsername,
                    newTemporaryPassword = result.NewTemporaryPassword
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error restoring from log {id}", ex, LogCategory.System);
                return StatusCode(500, new { success = false, message = "Error restoring record" });
            }
        }

        // GET: api/admin/AdminLog/dashboard-stats
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = await _logger.GetDashboardStatsAsync();

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
                        logsByDay,
                        errorsByDay
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
                // FIX: Use TryParse with ignoreCase for consistency with GetLogs
                LogLevel? parsedLevel = null;
                if (!string.IsNullOrEmpty(level))
                {
                    if (!Enum.TryParse<LogLevel>(level, ignoreCase: true, out var lv))
                        return BadRequest(new { success = false, message = $"Invalid log level: '{level}'" });
                    parsedLevel = lv;
                }

                LogCategory? parsedCategory = null;
                if (!string.IsNullOrEmpty(category))
                {
                    if (!Enum.TryParse<LogCategory>(category, ignoreCase: true, out var cat))
                        return BadRequest(new { success = false, message = $"Invalid category: '{category}'" });
                    parsedCategory = cat;
                }

                var filter = new LogQueryFilter
                {
                    Level = parsedLevel,
                    Category = parsedCategory,
                    StartDate = startDate,
                    EndDate = endDate,
                    PageSize = 50000
                };

                byte[] fileBytes;
                string fileName;
                string contentType;

                switch (format?.ToLower())
                {
                    case "csv":
                        fileBytes = await _logger.ExportLogsToCsvAsync(filter);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                        contentType = "text/csv";
                        break;

                    // FIX: Added 'excel' case — was previously unhandled, causing a
                    // 400 whenever Angular called exportLogs(filter, 'excel').
                    case "excel":
                        fileBytes = await _logger.ExportLogsToExcelAsync(filter);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
                        contentType = "application/vnd.ms-excel";
                        break;

                    case "json":
                        var json = await _logger.ExportLogsToJsonAsync(filter);
                        fileBytes = System.Text.Encoding.UTF8.GetBytes(json);
                        fileName = $"Logs_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                        contentType = "application/json";
                        break;

                    default:
                        return BadRequest(new { success = false, message = "Unsupported format. Supported: csv, excel, json" });
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
                        l.AdditionalData,
                        IsRestorable = !string.IsNullOrEmpty(l.TargetEntityType)
                                       && l.TargetEntityId != null
                                       && !string.IsNullOrEmpty(l.BeforeState)
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
                        dailyTrends,
                        topUsers,
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
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ClearOldLogs([FromQuery] int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
                var deletedCount = await _logger.ClearOldLogsAsync(cutoffDate);

                await _logger.LogInfoAsync(
                    $"Admin cleared {deletedCount} old logs (older than {daysToKeep} days)",
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