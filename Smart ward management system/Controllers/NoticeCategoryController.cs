using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model.Notice;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NoticeCategoryController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public NoticeCategoryController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: api/<NoticeCategoryController>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.NoticeCategories
                .Where(c => c.IsActive)
                .ToListAsync();

            return Ok(categories);
        }

        // GET api/<NoticeCategoryController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<NoticeCategoryController>
        [HttpPost]
        public async Task<IActionResult> CreateCategory(NoticeCategory category)
        {
            _context.NoticeCategories.Add(category);
            await _context.SaveChangesAsync();
            return Ok(category);
        }

        // PUT api/<NoticeCategoryController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<NoticeCategoryController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
