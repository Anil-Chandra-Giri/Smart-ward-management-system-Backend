using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Notice;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoticeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NoticeController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }
        // GET: api/<NoticeController>

        // GET api/<NoticeController>/5
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NoticeResponseDto>>> GetAllNotices()
        {
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
                    Category = n.Category!.Name,
                    FileUrl = n.FileUrl,
                    IsUrgent = n.IsUrgent,
                    PublishDate = n.PublishDate,
                    ExpiryDate = n.ExpiryDate
                })
                .OrderByDescending(n => n.PublishDate)
                .ToListAsync();

            return Ok(notices);
        }

        [HttpGet("urgent")]
        public async Task<IActionResult> GetUrgentNotices()
        {
            var notices = await _context.Notices
                .Where(n => n.IsUrgent && n.IsActive)
                .OrderByDescending(n => n.PublishDate)
                .ToListAsync();

            return Ok(notices);
        }

        // POST api/<NoticeController>
        [HttpPost]
        public async Task<IActionResult> CreateNotice([FromForm] CreateNoticeDTO dto)
        {
            string? filePath = null;

            if (dto.File != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "notices");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);

                var path = Path.Combine(folder, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await dto.File.CopyToAsync(stream);

                filePath = "/notices/" + fileName;
            }

            var notice = new Notice
            {
                Title = dto.Title,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                Type = dto.Type,
                FileUrl = filePath,
                ExpiryDate = dto.ExpiryDate,
                IsUrgent = dto.IsUrgent,
                CreatedBy = User.FindFirst("UserId")?.Value ?? "Admin"
            };

            _context.Notices.Add(notice);

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notice posted successfully" });
        }

        // PUT api/<NoticeController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNotice(Guid id, [FromForm] CreateNoticeDTO dto)
        {
            try
            {
                var notice = await _context.Notices.FindAsync(id);

                if (notice == null)
                    return NotFound(new { Error = "Notice not found" });

                // Update notice properties
                notice.Title = dto.Title;
                notice.Description = dto.Description;
                notice.CategoryId = dto.CategoryId;
                notice.Type = dto.Type;
                notice.ExpiryDate = dto.ExpiryDate;
                notice.IsUrgent = dto.IsUrgent;

                // Handle file upload if new file is provided
                if (dto.File != null && dto.File.Length > 0)
                {
                    // Delete old file if exists
                    if (!string.IsNullOrEmpty(notice.FileUrl))
                    {
                        var oldFilePath = Path.Combine(_env.WebRootPath, notice.FileUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    // Save new file
                    var folder = Path.Combine(_env.WebRootPath, "notices");
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    var fileName = Guid.NewGuid() + Path.GetExtension(dto.File.FileName);
                    var filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.File.CopyToAsync(stream);
                    }

                    notice.FileUrl = "/notices/" + fileName;
                }

                await _context.SaveChangesAsync();

                return Ok(new { Message = "Notice updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = "Failed to update notice", Details = ex.Message });
            }
        }

        // DELETE api/<NoticeController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotice(Guid id)
        {
            var notice = await _context.Notices.FindAsync(id);

            if (notice == null)
                return NotFound();

            notice.IsActive = false;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Notice deleted" });
        }
    }
}
