using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Volunteer;
using Smart_ward_management_system.Filters;
using Smart_ward_management_system.Model.Volunteer;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DisasterEventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DisasterEventsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/DisasterEvents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DisasterEventDto>>> GetDisasterEvents()
        {
            var events = await _context.DisasterEvents
                .Include(de => de.VolunteerAssignments)
                .Select(de => new DisasterEventDto
                {
                    Id = de.Id,
                    EventName = de.EventName,
                    EventType = de.EventType,
                    Description = de.Description,
                    Location = de.Location,
                    StartDate = de.StartDate,
                    EndDate = de.EndDate,
                    Severity = de.Severity,
                    Status = de.Status,
                    AffectedPeople = de.AffectedPeople,
                    RequiredResources = de.RequiredResources,
                    Coordinator = de.Coordinator,
                    ContactNumber = de.ContactNumber,
                    AssignedVolunteers = de.VolunteerAssignments.Count(va => va.Status == "Assigned" || va.Status == "InProgress")
                })
                .ToListAsync();

            return Ok(events);
        }

        // GET: api/DisasterEvents/active
        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<DisasterEventDto>>> GetActiveEvents()
        {
            var events = await _context.DisasterEvents
                .Where(de => de.Status == "Active")
                .Include(de => de.VolunteerAssignments)
                .Select(de => new DisasterEventDto
                {
                    Id = de.Id,
                    EventName = de.EventName,
                    EventType = de.EventType,
                    Description = de.Description,
                    Location = de.Location,
                    StartDate = de.StartDate,
                    EndDate = de.EndDate,
                    Severity = de.Severity,
                    Status = de.Status,
                    AffectedPeople = de.AffectedPeople,
                    RequiredResources = de.RequiredResources,
                    Coordinator = de.Coordinator,
                    ContactNumber = de.ContactNumber,
                    AssignedVolunteers = de.VolunteerAssignments.Count(va => va.Status == "Assigned" || va.Status == "InProgress")
                })
                .ToListAsync();

            return Ok(events);
        }

        // In your DisasterController.cs
        [HttpGet("registered-numbers")]
        public async Task<ActionResult<List<string>>> GetRegisteredPhoneNumbers()
        {
            var phoneNumbers = await _context.Users
                .Where(u => !string.IsNullOrEmpty(u.PhoneNumber)
                            && u.IsVerified) // Only verified users
                .Select(u => u.PhoneNumber)
                .ToListAsync();

            return Ok(phoneNumbers);
        }

        // GET: api/DisasterEvents/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DisasterEventDto>> GetDisasterEvent(Guid id)
        {
            var disasterEvent = await _context.DisasterEvents
                .Include(de => de.VolunteerAssignments)
                .Where(de => de.Id == id)
                .Select(de => new DisasterEventDto
                {
                    Id = de.Id,
                    EventName = de.EventName,
                    EventType = de.EventType,
                    Description = de.Description,
                    Location = de.Location,
                    StartDate = de.StartDate,
                    EndDate = de.EndDate,
                    Severity = de.Severity,
                    Status = de.Status,
                    AffectedPeople = de.AffectedPeople,
                    RequiredResources = de.RequiredResources,
                    Coordinator = de.Coordinator,
                    ContactNumber = de.ContactNumber,
                    AssignedVolunteers = de.VolunteerAssignments.Count(va => va.Status == "Assigned" || va.Status == "InProgress")
                })
                .FirstOrDefaultAsync();

            if (disasterEvent == null)
            {
                return NotFound();
            }

            return Ok(disasterEvent);
        }

        // POST: api/DisasterEvents
        [HttpPost]
        [RequireVerifiedCitizen]
        public async Task<ActionResult<DisasterEvent>> CreateDisasterEvent(CreateDisasterEventDto createDto)
        {
            var disasterEvent = new DisasterEvent
            {
                EventName = createDto.EventName,
                EventType = createDto.EventType,
                Description = createDto.Description,
                Location = createDto.Location,
                StartDate = createDto.StartDate,
                Severity = createDto.Severity,
                Status = "Active",
                AffectedPeople = createDto.AffectedPeople,
                RequiredResources = createDto.RequiredResources,
                Coordinator = createDto.Coordinator,
                ContactNumber = createDto.ContactNumber
            };

            _context.DisasterEvents.Add(disasterEvent);
            await _context.SaveChangesAsync();

            if (createDto.Severity == "High" || createDto.Severity == "Critical")
            {
                // Store the data needed for the background task
                var eventName = createDto.EventName;
                var location = createDto.Location;
                var severity = createDto.Severity;

                // Get a service scope factory
                var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Create a new scope for the background task
                        using (var scope = scopeFactory.CreateScope())
                        {
                            var smsService = scope.ServiceProvider.GetRequiredService<ISmsService>();
                            await smsService.SendDisasterAlertAsync(
                                eventName,
                                location,
                                severity
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        // Need to get logger from a new scope too
                        using (var scope = scopeFactory.CreateScope())
                        {
                            var logger = scope.ServiceProvider.GetRequiredService<ILogger<DisasterEventsController>>();
                            logger.LogError(ex, "Failed to send disaster alerts");
                        }
                    }
                });
            }

            return CreatedAtAction(nameof(GetDisasterEvent), new { id = disasterEvent.Id }, disasterEvent);
        }

        // PUT: api/DisasterEvents/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateDisasterEvent(Guid id, UpdateDisasterEventDto updateDto)
        {
            var disasterEvent = await _context.DisasterEvents.FindAsync(id);
            if (disasterEvent == null)
            {
                return NotFound();
            }

            disasterEvent.EventName = updateDto.EventName;
            disasterEvent.EventType = updateDto.EventType;
            disasterEvent.Description = updateDto.Description;
            disasterEvent.Location = updateDto.Location;
            disasterEvent.EndDate = updateDto.EndDate;
            disasterEvent.Severity = updateDto.Severity;
            disasterEvent.Status = updateDto.Status;
            disasterEvent.AffectedPeople = updateDto.AffectedPeople;
            disasterEvent.RequiredResources = updateDto.RequiredResources;
            disasterEvent.Coordinator = updateDto.Coordinator;
            disasterEvent.ContactNumber = updateDto.ContactNumber;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DisasterEventExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/DisasterEvents/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDisasterEvent(Guid id)
        {
            var disasterEvent = await _context.DisasterEvents.FindAsync(id);
            if (disasterEvent == null)
            {
                return NotFound();
            }

            _context.DisasterEvents.Remove(disasterEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DisasterEventExists(Guid id)
        {
            return _context.DisasterEvents.Any(e => e.Id == id);
        }
    }
}
