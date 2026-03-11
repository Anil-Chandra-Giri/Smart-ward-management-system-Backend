using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Appointment;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<RealTimeHub> _hubContext;
        public AppointmentController(ApplicationDbContext context, IHubContext<RealTimeHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }
        // GET: api/<AppointmentController>
        [HttpGet("queue/{wardNumber}")]
        public async Task<IActionResult> GetQueueByWard(int wardNumber)
        {
            // Get all appointments in queue for the ward
            var queue = await _context.Queues
                .Where(q => q.WardNumber == wardNumber && q.Status == "In Queue")
                .OrderBy(q => q.CreatedAt)
                .ToListAsync();

            return Ok(queue);
        }


        [Route("GetAllAppointments")]
        [HttpGet]
        public async Task<IActionResult> GetAllAppointments()
        {
            var appointments = await _context.Appointments.ToListAsync();
            return Ok(appointments);
        }

        // GET api/<AppointmentController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<AppointmentController>
        [HttpPost("book")]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentDto appointment)
        {
            if (appointment == null)
                return BadRequest("Invalid appointment data.");

            var newAppointment = new Appointment
            {
                AppointmentId = Guid.NewGuid(),
                CitizenName = appointment.CitizenName,
                ContactNumber = appointment.ContactNumber,
                ServiceType = appointment.ServiceType,
                WardNumber = appointment.WardNumber,
                AppointmentTime = appointment.AppointmentTime,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(newAppointment);
            await _context.SaveChangesAsync();

            var token = new Token
            {
                TokenId = Guid.NewGuid(),
                AppointmentId = newAppointment.AppointmentId,
                Status = "Active",
                IssuedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tokens.Add(token);

            await _context.SaveChangesAsync();

            string tokenNumber = $"TKN-{token.TokenSequence:D3}";

            token.TokenNumber = tokenNumber;
            newAppointment.TokenNumber = tokenNumber;

            var queue = new Queue
            {
                QueueId = Guid.NewGuid(),
                WardNumber = newAppointment.WardNumber,
                TokenNumber = tokenNumber,
                CitizenName = newAppointment.CitizenName,
                ServiceType = newAppointment.ServiceType,
                Status = "In Queue",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Queues.Add(queue);
            await _context.SaveChangesAsync();

            await _context.SaveChangesAsync();

            return Ok(new
            {
                newAppointment.AppointmentId,
                TokenNumber = tokenNumber
            });
        }

        // PUT api/<AppointmentController>/5
        [HttpPut("queue/update/{tokenNumber}")]
        public async Task<IActionResult> UpdateQueueStatus(string tokenNumber, [FromBody] string status)
        {
            // Find the token in the queue
            var queue = await _context.Queues
                .FirstOrDefaultAsync(q => q.TokenNumber == tokenNumber);

            if (queue == null)
            {
                return NotFound("Queue entry not found.");
            }

            queue.Status = status;
            queue.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveTokenUpdate", tokenNumber, status);

            return Ok(queue);
        }

        // DELETE api/<AppointmentController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
