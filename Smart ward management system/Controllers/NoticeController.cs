using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Notice;
using Smart_ward_management_system.Services;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoticeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ILoggingService _logger;

        public NoticeController(ApplicationDbContext context, IWebHostEnvironment env, ILoggingService logger)
        {
            _context = context;
            _env = env;
            _logger = logger;
        }

        // GET: api/<NoticeController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoticeResponseDto>>> GetAllNotices()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching all active notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });

                var now = DateTime.UtcNow;

                var notices = await _context.Notices
                    .Include(n => n.Category)
                    .Where(n => n.IsActive &&
                           (n.ExpiryDate == null || n.ExpiryDate > now))
                    .Select(n => new NoticeResponseDto
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Description = n.Description,
                        Category = n.Category != null ? n.Category.Name : string.Empty,
                        FileUrl = n.FileUrl,
                        IsUrgent = n.IsUrgent,
                        PublishDate = n.PublishDate,
                        ExpiryDate = n.ExpiryDate
                    })
                    .OrderByDescending(n => n.PublishDate)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {notices.Count} active notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Count = notices.Count });

                return Ok(notices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching all notices",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching notices." });
            }
        }

        [HttpGet("urgent")]
        public async Task<IActionResult> GetUrgentNotices()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching urgent notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });

                var notices = await _context.Notices
                    .Where(n => n.IsUrgent && n.IsActive)
                    .OrderByDescending(n => n.PublishDate)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {notices.Count} urgent notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Count = notices.Count });

                return Ok(notices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching urgent notices",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching urgent notices." });
            }
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetNoticesByCategory(Guid categoryId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching notices by category: {categoryId}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, CategoryId = categoryId });

                var now = DateTime.UtcNow;
                var notices = await _context.Notices
                    .Include(n => n.Category)
                    .Where(n => n.CategoryId == categoryId && n.IsActive &&
                           (n.ExpiryDate == null || n.ExpiryDate > now))
                    .OrderByDescending(n => n.PublishDate)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {notices.Count} notices for category {categoryId}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, CategoryId = categoryId, Count = notices.Count });

                return Ok(notices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching notices by category: {categoryId}",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, CategoryId = categoryId });
                return StatusCode(500, new { message = "An error occurred while fetching notices by category." });
            }
        }

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveNotices()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching all active notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });

                var now = DateTime.UtcNow;
                var notices = await _context.Notices
                    .Include(n => n.Category)
                    .Where(n => n.IsActive && (n.ExpiryDate == null || n.ExpiryDate > now))
                    .OrderByDescending(n => n.PublishDate)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {notices.Count} active notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Count = notices.Count });

                return Ok(notices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching active notices",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching active notices." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetNoticeById(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching notice by ID: {id}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });

                var notice = await _context.Notices
                    .Include(n => n.Category)
                    .FirstOrDefaultAsync(n => n.Id == id);

                if (notice == null)
                {
                    await _logger.LogWarningAsync($"Notice not found with ID: {id}",
                        LogCategory.Notifications,
                        new { CorrelationId = correlationId, NoticeId = id });
                    return NotFound(new { message = "Notice not found" });
                }

                await _logger.LogInfoAsync($"Retrieved notice {id}: {notice.Title}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id, Title = notice.Title });

                return Ok(notice);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching notice by ID: {id}",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });
                return StatusCode(500, new { message = "An error occurred while fetching the notice." });
            }
        }

        // POST api/<NoticeController>
        [HttpPost]
        public async Task<IActionResult> CreateNotice([FromForm] CreateNoticeDTO dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var userId = User.FindFirst("UserId")?.Value ?? "Anonymous";

                await _logger.LogInfoAsync($"Creating new notice: {dto.Title}",
                    LogCategory.Notifications,
                    new
                    {
                        CorrelationId = correlationId,
                        Title = dto.Title,
                        CategoryId = dto.CategoryId,
                        IsUrgent = dto.IsUrgent,
                        CreatedBy = userId
                    });

                string? filePath = null;

                if (dto.File != null)
                {
                    var folder = Path.Combine(_env.WebRootPath, "notices");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.File.FileName);
                    var path = Path.Combine(folder, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await dto.File.CopyToAsync(stream);

                    filePath = "/notices/" + fileName;

                    await _logger.LogInfoAsync($"File uploaded for notice: {fileName}",
                        LogCategory.Notifications,
                        new { CorrelationId = correlationId, FileName = fileName, FilePath = filePath });
                }

                // Convert string Type to NoticeType enum
                NoticeType noticeType = NoticeType.General;
                if (!string.IsNullOrEmpty(dto.Type))
                {
                    if (Enum.TryParse<NoticeType>(dto.Type, true, out var parsedType))
                    {
                        noticeType = parsedType;
                    }
                }

                var notice = new Notice
                {
                    Id = Guid.NewGuid(),
                    Title = dto.Title,
                    Description = dto.Description,
                    CategoryId = dto.CategoryId,
                    Type = noticeType,
                    FileUrl = filePath,
                    ExpiryDate = dto.ExpiryDate,
                    IsUrgent = dto.IsUrgent,
                    IsActive = true,
                    PublishDate = DateTime.UtcNow,
                    CreatedBy = userId
                };

                _context.Notices.Add(notice);
                await _context.SaveChangesAsync();

                // Log the notice creation
                await _logger.LogNoticeActionAsync(notice.Id, "created");

                await _logger.LogInfoAsync($"Notice created successfully with ID: {notice.Id}",
                    LogCategory.Notifications,
                    new
                    {
                        CorrelationId = correlationId,
                        NoticeId = notice.Id,
                        Title = notice.Title,
                        CreatedBy = userId
                    });

                return Ok(new
                {
                    Message = "Notice posted successfully",
                    NoticeId = notice.Id,
                    Title = notice.Title
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error creating notice: {dto?.Title}",
                    ex,
                    LogCategory.Notifications,
                    new
                    {
                        CorrelationId = correlationId,
                        Title = dto?.Title,
                        CategoryId = dto?.CategoryId,
                        ErrorMessage = ex.Message
                    });
                return StatusCode(500, new { message = "An error occurred while creating the notice." });
            }
        }

        // PUT api/<NoticeController>/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateNotice(Guid id, [FromForm] UpdateNoticeDTO dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Updating notice: {id}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });

                var notice = await _context.Notices.FindAsync(id);
                if (notice == null)
                {
                    await _logger.LogWarningAsync($"Notice not found for update: {id}",
                        LogCategory.Notifications,
                        new { CorrelationId = correlationId, NoticeId = id });
                    return NotFound(new { message = "Notice not found" });
                }

                var oldTitle = notice.Title;
                var oldIsUrgent = notice.IsUrgent;

                // Update fields
                if (!string.IsNullOrEmpty(dto.Title))
                    notice.Title = dto.Title;

                if (!string.IsNullOrEmpty(dto.Description))
                    notice.Description = dto.Description;

                if (dto.CategoryId.HasValue)
                    notice.CategoryId = dto.CategoryId.Value;

                if (!string.IsNullOrEmpty(dto.Type))
                {
                    if (Enum.TryParse<NoticeType>(dto.Type, true, out var parsedType))
                    {
                        notice.Type = parsedType;
                    }
                }

                if (dto.ExpiryDate.HasValue)
                    notice.ExpiryDate = dto.ExpiryDate.Value;

                if (dto.IsUrgent.HasValue)
                    notice.IsUrgent = dto.IsUrgent.Value;

                // Handle file update
                if (dto.File != null)
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(notice.FileUrl))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, notice.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    var folder = Path.Combine(_env.WebRootPath, "notices");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.File.FileName);
                    var path = Path.Combine(folder, fileName);

                    using var stream = new FileStream(path, FileMode.Create);
                    await dto.File.CopyToAsync(stream);

                    notice.FileUrl = "/notices/" + fileName;
                }

                await _context.SaveChangesAsync();

                await _logger.LogInfoAsync($"Notice updated: {id} - Title changed from '{oldTitle}' to '{notice.Title}'",
                    LogCategory.Notifications,
                    new
                    {
                        CorrelationId = correlationId,
                        NoticeId = id,
                        OldTitle = oldTitle,
                        NewTitle = notice.Title,
                        OldIsUrgent = oldIsUrgent,
                        NewIsUrgent = notice.IsUrgent
                    });

                return Ok(new { message = "Notice updated successfully", notice });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating notice: {id}",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });
                return StatusCode(500, new { message = "An error occurred while updating the notice." });
            }
        }

        // DELETE api/<NoticeController>/delete/{id}
        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteNotice(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogWarningAsync($"Attempting to delete notice: {id}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });

                var notice = await _context.Notices.FindAsync(id);

                if (notice == null)
                {
                    await _logger.LogWarningAsync($"Notice not found for deletion: {id}",
                        LogCategory.Notifications,
                        new { CorrelationId = correlationId, NoticeId = id });
                    return NotFound(new { message = "Notice not found" });
                }

                // Soft delete - mark as inactive
                notice.IsActive = false;
                await _context.SaveChangesAsync();

                await _logger.LogWarningAsync($"Notice soft deleted: {id} - Title: {notice.Title}",
                    LogCategory.Notifications,
                    new
                    {
                        CorrelationId = correlationId,
                        NoticeId = id,
                        Title = notice.Title
                    });

                return Ok(new { message = "Notice deleted successfully" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deleting notice: {id}",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });
                return StatusCode(500, new { message = "An error occurred while deleting the notice." });
            }
        }

        // DELETE api/<NoticeController>/permanent/{id}
        [HttpDelete("permanent/{id}")]
        public async Task<IActionResult> PermanentDeleteNotice(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogWarningAsync($"Attempting to permanently delete notice: {id}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });

                var notice = await _context.Notices.FindAsync(id);

                if (notice == null)
                {
                    await _logger.LogWarningAsync($"Notice not found for permanent deletion: {id}",
                        LogCategory.Notifications,
                        new { CorrelationId = correlationId, NoticeId = id });
                    return NotFound(new { message = "Notice not found" });
                }

                // Delete associated file if exists
                if (!string.IsNullOrEmpty(notice.FileUrl))
                {
                    var filePath = Path.Combine(_env.WebRootPath, notice.FileUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                    {
                        System.IO.File.Delete(filePath);
                        await _logger.LogInfoAsync($"Deleted file associated with notice: {notice.FileUrl}",
                            LogCategory.Notifications,
                            new { CorrelationId = correlationId, FilePath = notice.FileUrl });
                    }
                }

                _context.Notices.Remove(notice);
                await _context.SaveChangesAsync();

                await _logger.LogWarningAsync($"Notice permanently deleted: {id} - Title: {notice.Title}",
                    LogCategory.Notifications,
                    new
                    {
                        CorrelationId = correlationId,
                        NoticeId = id,
                        Title = notice.Title
                    });

                return Ok(new { message = "Notice permanently deleted" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error permanently deleting notice: {id}",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, NoticeId = id });
                return StatusCode(500, new { message = "An error occurred while deleting the notice." });
            }
        }

        // GET: api/<NoticeController>/expired
        [HttpGet("expired")]
        public async Task<IActionResult> GetExpiredNotices()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching expired notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });

                var now = DateTime.UtcNow;
                var expiredNotices = await _context.Notices
                    .Where(n => n.ExpiryDate != null && n.ExpiryDate <= now)
                    .OrderByDescending(n => n.ExpiryDate)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {expiredNotices.Count} expired notices",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Count = expiredNotices.Count });

                return Ok(expiredNotices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching expired notices",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching expired notices." });
            }
        }

        // GET: api/<NoticeController>/search
        [HttpGet("search")]
        public async Task<IActionResult> SearchNotices([FromQuery] string keyword)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Searching notices with keyword: {keyword}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Keyword = keyword });

                if (string.IsNullOrEmpty(keyword))
                {
                    return BadRequest(new { message = "Keyword is required" });
                }

                var now = DateTime.UtcNow;
                var notices = await _context.Notices
                    .Include(n => n.Category)
                    .Where(n => n.IsActive &&
                           (n.ExpiryDate == null || n.ExpiryDate > now) &&
                           (n.Title.Contains(keyword) || n.Description.Contains(keyword)))
                    .OrderByDescending(n => n.PublishDate)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Found {notices.Count} notices matching keyword: {keyword}",
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Keyword = keyword, Count = notices.Count });

                return Ok(notices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error searching notices with keyword: {keyword}",
                    ex,
                    LogCategory.Notifications,
                    new { CorrelationId = correlationId, Keyword = keyword });
                return StatusCode(500, new { message = "An error occurred while searching notices." });
            }
        }
    }
}