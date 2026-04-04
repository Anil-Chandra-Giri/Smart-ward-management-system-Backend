using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Volunteer;
using Smart_ward_management_system.Model.Volunteer;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VolunteerAssignmentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VolunteerAssignmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/VolunteerAssignments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetAssignments()
        {
            var assignments = await _context.VolunteerAssignments
                .Include(va => va.Volunteer)
                .Include(va => va.DisasterEvent)
                .Select(va => new VolunteerAssignmentDto
                {
                    Id = va.Id,
                    VolunteerId = va.VolunteerId,
                    VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
                    DisasterEventId = va.DisasterEventId,
                    DisasterEventName = va.DisasterEvent.EventName,
                    Role = va.Role,
                    AssignedDate = va.AssignedDate,
                    StartDate = va.StartDate,
                    EndDate = va.EndDate,
                    Status = va.Status,
                    Notes = va.Notes
                })
                .ToListAsync();

            return Ok(assignments);
        }

        // GET: api/VolunteerAssignments/volunteer/{volunteerId}
        [HttpGet("volunteer/{volunteerId}")]
        public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetAssignmentsByVolunteer(Guid volunteerId)
        {
            var assignments = await _context.VolunteerAssignments
                .Include(va => va.Volunteer)
                .Include(va => va.DisasterEvent)
                .Where(va => va.VolunteerId == volunteerId)
                .Select(va => new VolunteerAssignmentDto
                {
                    Id = va.Id,
                    VolunteerId = va.VolunteerId,
                    VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
                    DisasterEventId = va.DisasterEventId,
                    DisasterEventName = va.DisasterEvent.EventName,
                    Role = va.Role,
                    AssignedDate = va.AssignedDate,
                    StartDate = va.StartDate,
                    EndDate = va.EndDate,
                    Status = va.Status,
                    Notes = va.Notes
                })
                .ToListAsync();

            return Ok(assignments);
        }

        // GET: api/VolunteerAssignments/event/{eventId}
        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetAssignmentsByEvent(Guid eventId)
        {
            var assignments = await _context.VolunteerAssignments
                .Include(va => va.Volunteer)
                .Include(va => va.DisasterEvent)
                .Where(va => va.DisasterEventId == eventId)
                .Select(va => new VolunteerAssignmentDto
                {
                    Id = va.Id,
                    VolunteerId = va.VolunteerId,
                    VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
                    DisasterEventId = va.DisasterEventId,
                    DisasterEventName = va.DisasterEvent.EventName,
                    Role = va.Role,
                    AssignedDate = va.AssignedDate,
                    StartDate = va.StartDate,
                    EndDate = va.EndDate,
                    Status = va.Status,
                    Notes = va.Notes
                })
                .ToListAsync();

            return Ok(assignments);
        }

        // GET: api/VolunteerAssignments/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VolunteerAssignmentDto>> GetAssignment(Guid id)
        {
            var assignment = await _context.VolunteerAssignments
                .Include(va => va.Volunteer)
                .Include(va => va.DisasterEvent)
                .Where(va => va.Id == id)
                .Select(va => new VolunteerAssignmentDto
                {
                    Id = va.Id,
                    VolunteerId = va.VolunteerId,
                    VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
                    DisasterEventId = va.DisasterEventId,
                    DisasterEventName = va.DisasterEvent.EventName,
                    Role = va.Role,
                    AssignedDate = va.AssignedDate,
                    StartDate = va.StartDate,
                    EndDate = va.EndDate,
                    Status = va.Status,
                    Notes = va.Notes
                })
                .FirstOrDefaultAsync();

            if (assignment == null)
            {
                return NotFound();
            }

            return Ok(assignment);
        }

        // POST: api/VolunteerAssignments
        [HttpPost]
        public async Task<ActionResult<VolunteerAssignment>> CreateAssignment(CreateVolunteerAssignmentDto createDto)
        {
            var assignment = new VolunteerAssignment
            {
                VolunteerId = createDto.VolunteerId,
                DisasterEventId = createDto.DisasterEventId,
                Role = createDto.Role,
                StartDate = createDto.StartDate,
                Notes = createDto.Notes,
                Status = "Assigned"
            };

            _context.VolunteerAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAssignment), new { id = assignment.Id }, assignment);
        }

        // PUT: api/VolunteerAssignments/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAssignment(Guid id, UpdateVolunteerAssignmentDto updateDto)
        {
            var assignment = await _context.VolunteerAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            assignment.Role = updateDto.Role;
            assignment.StartDate = updateDto.StartDate;
            assignment.EndDate = updateDto.EndDate;
            assignment.Status = updateDto.Status;
            assignment.Notes = updateDto.Notes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(id))
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

        // PATCH: api/VolunteerAssignments/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateAssignmentStatus(Guid id, [FromBody] string status)
        {
            var assignment = await _context.VolunteerAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            assignment.Status = status;
            if (status == "Completed")
            {
                assignment.EndDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/VolunteerAssignments/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssignment(Guid id)
        {
            var assignment = await _context.VolunteerAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            _context.VolunteerAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssignmentExists(Guid id)
        {
            return _context.VolunteerAssignments.Any(e => e.Id == id);
        }
    }
}
