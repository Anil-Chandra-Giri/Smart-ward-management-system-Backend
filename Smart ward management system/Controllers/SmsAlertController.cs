using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model.Volunteer;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SmsAlertsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SmsAlertsController> _logger;

        public SmsAlertsController(ApplicationDbContext context, ILogger<SmsAlertsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/SmsAlerts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SmsAlert>>> GetSmsAlerts()
        {
            return await _context.SmsAlerts
                .Include(sa => sa.DisasterEvent)
                .OrderByDescending(sa => sa.SentDate)
                .ToListAsync();
        }

        // GET: api/SmsAlerts/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SmsAlert>> GetSmsAlert(Guid id)
        {
            var smsAlert = await _context.SmsAlerts
                .Include(sa => sa.DisasterEvent)
                .FirstOrDefaultAsync(sa => sa.Id == id);

            if (smsAlert == null)
            {
                return NotFound();
            }

            return smsAlert;
        }

        // POST: api/SmsAlerts/send-to-all-volunteers
        [HttpPost("send-to-all-volunteers")]
        public async Task<ActionResult<SmsAlert>> SendToAllVolunteers([FromBody] SmsRequest request)
        {
            var volunteers = await _context.Volunteers
                .Where(v => v.IsActive)
                .ToListAsync();

            var smsAlert = new SmsAlert
            {
                Message = request.Message,
                RecipientGroup = "All Volunteers",
                Status = "Sending",
                RecipientCount = volunteers.Count,
                DisasterEventId = request.DisasterEventId
            };

            _context.SmsAlerts.Add(smsAlert);
            await _context.SaveChangesAsync();

            // In a real application, you would integrate with an SMS service here
            // For demo purposes, we'll simulate sending
            _ = Task.Run(async () =>
            {
                try
                {
                    int successCount = 0;
                    int failedCount = 0;

                    foreach (var volunteer in volunteers)
                    {
                        try
                        {
                            // Simulate sending SMS
                            _logger.LogInformation($"Sending SMS to {volunteer.PhoneNumber}: {request.Message}");
                            // await _smsService.SendAsync(volunteer.PhoneNumber, request.Message);
                            successCount++;
                            await Task.Delay(100); // Simulate network delay
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send SMS to {volunteer.PhoneNumber}");
                            failedCount++;
                        }
                    }

                    // Update alert status
                    var alert = await _context.SmsAlerts.FindAsync(smsAlert.Id);
                    if (alert != null)
                    {
                        alert.Status = "Sent";
                        alert.SuccessCount = successCount;
                        alert.FailedCount = failedCount;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in SMS sending process");

                    var alert = await _context.SmsAlerts.FindAsync(smsAlert.Id);
                    if (alert != null)
                    {
                        alert.Status = "Failed";
                        await _context.SaveChangesAsync();
                    }
                }
            });

            return Ok(smsAlert);
        }

        // POST: api/SmsAlerts/send-to-event-volunteers/{eventId}
        [HttpPost("send-to-event-volunteers/{eventId}")]
        public async Task<ActionResult<SmsAlert>> SendToEventVolunteers(Guid eventId, [FromBody] SmsRequest request)
        {
            var assignments = await _context.VolunteerAssignments
                .Include(va => va.Volunteer)
                .Where(va => va.DisasterEventId == eventId && va.Status != "Completed")
                .ToListAsync();

            var volunteers = assignments.Select(a => a.Volunteer).Distinct().ToList();

            var smsAlert = new SmsAlert
            {
                Message = request.Message,
                RecipientGroup = $"Event Volunteers - {eventId}",
                Status = "Sending",
                RecipientCount = volunteers.Count,
                DisasterEventId = eventId
            };

            _context.SmsAlerts.Add(smsAlert);
            await _context.SaveChangesAsync();

            // Simulate sending SMS (same as above)
            _ = Task.Run(async () =>
            {
                try
                {
                    int successCount = 0;
                    int failedCount = 0;

                    foreach (var volunteer in volunteers)
                    {
                        try
                        {
                            _logger.LogInformation($"Sending SMS to {volunteer.PhoneNumber}: {request.Message}");
                            successCount++;
                            await Task.Delay(100);
                        }
                        catch
                        {
                            failedCount++;
                        }
                    }

                    var alert = await _context.SmsAlerts.FindAsync(smsAlert.Id);
                    if (alert != null)
                    {
                        alert.Status = "Sent";
                        alert.SuccessCount = successCount;
                        alert.FailedCount = failedCount;
                        await _context.SaveChangesAsync();
                    }
                }
                catch
                {
                    var alert = await _context.SmsAlerts.FindAsync(smsAlert.Id);
                    if (alert != null)
                    {
                        alert.Status = "Failed";
                        await _context.SaveChangesAsync();
                    }
                }
            });

            return Ok(smsAlert);
        }

        public class SmsRequest
        {
            public string Message { get; set; }
            public Guid? DisasterEventId { get; set; }
        }
    }
}
