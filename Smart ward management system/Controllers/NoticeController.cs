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
        public async Task<IActionResult> GetAllNotices()
        {
            var now = DateTime.UtcNow;

            var notices = await _context.Notices
                .Include(n => n.Category)
                .Where(n => n.IsActive &&
                       (n.ExpiryDate == null || n.ExpiryDate > now))
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Description,
                    Category = n.Category!.Name,
                    n.FileUrl,
                    n.IsUrgent,
                    n.PublishDate,
                    n.ExpiryDate
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
        public void Put(int id, [FromBody] string value)
        {
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

            return Ok("Notice deleted");
        }
    }
}
