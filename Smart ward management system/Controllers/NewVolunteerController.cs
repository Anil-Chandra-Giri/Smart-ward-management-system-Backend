using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Volunteer;
using Smart_ward_management_system.Model.Volunteer;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VolunteerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VolunteerDto>>> GetVolunteers()
        {
            var volunteers = await _context.Volunteers
                .Include(v => v.Assignments)
                .Select(v => new VolunteerDto
                {
                    Id = v.Id,
                    FirstName = v.FirstName,
                    LastName = v.LastName,
                    Email = v.Email,
                    PhoneNumber = v.PhoneNumber,
                    Address = v.Address,
                    DateOfBirth = v.DateOfBirth,
                    Skills = v.Skills,
                    Availability = v.Availability,
                    IsActive = v.IsActive,
                    RegistrationDate = v.RegistrationDate,
                    EmergencyContact = v.EmergencyContact,
                    EmergencyPhone = v.EmergencyPhone,
                    ProfilePicture = v.ProfilePicture,
                    ActiveAssignments = v.Assignments
                        .Count(a => a.Status == "Assigned" || a.Status == "InProgress")
                })
                .ToListAsync();

            return Ok(volunteers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VolunteerDto>> GetVolunteer(Guid id)
        {
            var v = await _context.Volunteers
                .Include(v => v.Assignments)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (v == null) return NotFound();

            return Ok(new VolunteerDto
            {
                Id = v.Id,
                FirstName = v.FirstName,
                LastName = v.LastName,
                Email = v.Email,
                PhoneNumber = v.PhoneNumber,
                Address = v.Address,
                DateOfBirth = v.DateOfBirth,
                Skills = v.Skills,
                Availability = v.Availability,
                IsActive = v.IsActive,
                RegistrationDate = v.RegistrationDate,
                EmergencyContact = v.EmergencyContact,
                EmergencyPhone = v.EmergencyPhone,
                ProfilePicture = v.ProfilePicture,
                ActiveAssignments = v.Assignments
                    .Count(a => a.Status == "Assigned" || a.Status == "InProgress")
            });
        }

        [HttpGet("{id}/assignments")]
        public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetVolunteerAssignments(Guid id)
        {
            if (!await _context.Volunteers.AnyAsync(v => v.Id == id))
                return NotFound();

            var assignments = await _context.VolunteerAssignments
                .Include(va => va.Volunteer)
                .Include(va => va.DisasterEvent)
                .Where(va => va.VolunteerId == id)
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

        [HttpPost]
        public async Task<ActionResult<VolunteerDto>> CreateVolunteer([FromForm] CreateVolunteerDto dto)
        {
            if (await _context.Volunteers.AnyAsync(v => v.Email == dto.Email))
                return Conflict(new { message = "A volunteer with this email already exists." });

            if (await _context.Volunteers.AnyAsync(v => v.PhoneNumber == dto.PhoneNumber))
                return Conflict(new { message = "A volunteer with this phone number already exists." });

            string? picturePath = null;
            if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "volunteers");
                Directory.CreateDirectory(uploadsFolder);
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await dto.ProfilePicture.CopyToAsync(stream);
                picturePath = $"/uploads/volunteers/{fileName}";
            }

            var volunteer = new Volunteer
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                DateOfBirth = dto.DateOfBirth,
                Skills = dto.Skills,
                Availability = dto.Availability,
                EmergencyContact = dto.EmergencyContact,
                EmergencyPhone = dto.EmergencyPhone,
                ProfilePicture = picturePath,
                IsActive = true,
                RegistrationDate = DateTime.UtcNow
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVolunteer), new { id = volunteer.Id }, new VolunteerDto
            {
                Id = volunteer.Id,
                FirstName = volunteer.FirstName,
                LastName = volunteer.LastName,
                Email = volunteer.Email,
                PhoneNumber = volunteer.PhoneNumber,
                Address = volunteer.Address,
                DateOfBirth = volunteer.DateOfBirth,
                Skills = volunteer.Skills,
                Availability = volunteer.Availability,
                IsActive = volunteer.IsActive,
                RegistrationDate = volunteer.RegistrationDate,
                EmergencyContact = volunteer.EmergencyContact,
                EmergencyPhone = volunteer.EmergencyPhone,
                ProfilePicture = volunteer.ProfilePicture,
                ActiveAssignments = 0
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVolunteer(Guid id, UpdateVolunteerDto dto)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null) return NotFound();

            if (await _context.Volunteers.AnyAsync(v => v.Email == dto.Email && v.Id != id))
                return Conflict(new { message = "A volunteer with this email already exists." });

            if (await _context.Volunteers.AnyAsync(v => v.PhoneNumber == dto.PhoneNumber && v.Id != id))
                return Conflict(new { message = "A volunteer with this phone number already exists." });

            volunteer.FirstName = dto.FirstName;
            volunteer.LastName = dto.LastName;
            volunteer.Email = dto.Email;
            volunteer.PhoneNumber = dto.PhoneNumber;
            volunteer.Address = dto.Address;
            volunteer.Skills = dto.Skills;
            volunteer.Availability = dto.Availability;
            volunteer.IsActive = dto.IsActive;
            volunteer.EmergencyContact = dto.EmergencyContact;
            volunteer.EmergencyPhone = dto.EmergencyPhone;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVolunteer(Guid id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null) return NotFound();

            var hasActive = await _context.VolunteerAssignments
                .AnyAsync(va => va.VolunteerId == id &&
                               (va.Status == "Assigned" || va.Status == "InProgress"));
            if (hasActive)
                return BadRequest(new { message = "Cannot delete a volunteer with active assignments." });

            _context.Volunteers.Remove(volunteer);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("self-register")]
        public async Task<ActionResult<SelfRegisterResponseDto>> SelfRegister(
            SelfRegisterVolunteerDto dto)
        {
            var disasterEvent = await _context.DisasterEvents.FindAsync(dto.DisasterEventId);
            if (disasterEvent == null)
                return NotFound(new { message = "Disaster event not found." });

            var volunteer = await _context.Volunteers
                .FirstOrDefaultAsync(v => v.Email == dto.Email);

            if (volunteer == null)
            {
                if (await _context.Volunteers.AnyAsync(v => v.PhoneNumber == dto.PhoneNumber))
                    return Conflict(new { message = "A volunteer with this phone number already exists." });

                volunteer = new Volunteer
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    Address = dto.Address,
                    DateOfBirth = dto.DateOfBirth ?? DateTime.MinValue,
                    Skills = dto.Skills,
                    Availability = dto.Availability,
                    EmergencyContact = dto.EmergencyContact,
                    EmergencyPhone = dto.EmergencyPhone,
                    ProfilePicture = "coming",
                    IsActive = true,
                    RegistrationDate = DateTime.UtcNow
                };

                _context.Volunteers.Add(volunteer);
                await _context.SaveChangesAsync();
            }

            var alreadyAssigned = await _context.VolunteerAssignments.AnyAsync(va =>
                va.VolunteerId == volunteer.Id &&
                va.DisasterEventId == dto.DisasterEventId &&
                va.Status != "Cancelled" &&
                va.Status != "Completed");

            if (alreadyAssigned)
                return Conflict(new { message = "You are already registered as a volunteer for this event." });

            var assignment = new VolunteerAssignment
            {
                VolunteerId = volunteer.Id,
                DisasterEventId = dto.DisasterEventId,
                Role = "General Volunteer",
                AssignedDate = DateTime.UtcNow,
                Status = "Assigned",
                Notes = dto.Notes
            };

            _context.VolunteerAssignments.Add(assignment);
            await _context.SaveChangesAsync();

            return Ok(new SelfRegisterResponseDto
            {
                VolunteerId = volunteer.Id,
                AssignmentId = assignment.Id,
                Message = $"You have been successfully registered as a volunteer for '{disasterEvent.EventName}'."
            });
        }
    }
}