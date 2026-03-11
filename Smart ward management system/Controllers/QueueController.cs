using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model.Appointment;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public QueueController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: api/<QueueController>
        [HttpGet("ward/{wardNumber}")]
        public async Task<IActionResult> GetQueueByWard(int wardNumber)
        {
            
            var queue = await _context.Queues
                .Where(q => q.WardNumber == wardNumber && q.Status == "In Queue")
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();

            return Ok(queue);
        }

        // GET api/<QueueController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<QueueController>
        [HttpPost("add")]
        public async Task<IActionResult> AddToQueue([FromBody] Appointment appointment)
        {
            if (appointment == null)
            {
                return BadRequest("Invalid appointment data.");
            }

            // Add the appointment to the queue
            var queue = new Queue
            {
                WardNumber = appointment.WardNumber,
                TokenNumber = appointment.TokenNumber,
                CitizenName = appointment.CitizenName,
                ServiceType = appointment.ServiceType,
                Status = "In Queue",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Queues.Add(queue);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(AddToQueue), new { id = queue.QueueId }, queue);
        }

        // PUT api/<QueueController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<QueueController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
