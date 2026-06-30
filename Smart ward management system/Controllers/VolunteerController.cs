using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Volunteer;
using Smart_ward_management_system.Filters;
using Smart_ward_management_system.Model.Volunteer;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VolunteersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public VolunteersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Volunteers
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
                    ProfilePicture = v.ProfilePicture??string.Empty,
                    ActiveAssignments = v.Assignments.Count(a => a.Status == "InProgress" || a.Status == "Assigned")
                })
                .ToListAsync();

            return Ok(volunteers);
        }

        // GET: api/Volunteers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<VolunteerDto>> GetVolunteer(Guid id)
        {
            var volunteer = await _context.Volunteers
                .Include(v => v.Assignments)
                .Where(v => v.Id == id)
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
                    ActiveAssignments = v.Assignments.Count(a => a.Status == "InProgress" || a.Status == "Assigned")
                })
                .FirstOrDefaultAsync();

            if (volunteer == null)
            {
                return NotFound();
            }

            return Ok(volunteer);
        }

        // POST: api/Volunteers
        [HttpPost]
        [RequireVerifiedCitizen]
        public async Task<ActionResult<Volunteer>> CreateVolunteer(CreateVolunteerDto createDto, IFormFile profilePicture)
        {
            string? fileName = null;

            if (createDto.ProfilePicture != null)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                fileName = Guid.NewGuid().ToString() + Path.GetExtension(createDto.ProfilePicture.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await createDto.ProfilePicture.CopyToAsync(stream);
                }
            }

            var volunteer = new Volunteer
            {
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                Email = createDto.Email,
                PhoneNumber = createDto.PhoneNumber,
                Address = createDto.Address,
                DateOfBirth = createDto.DateOfBirth,
                Skills = createDto.Skills,
                Availability = createDto.Availability,
                EmergencyContact = createDto.EmergencyContact,
                EmergencyPhone = createDto.EmergencyPhone,
                IsActive = true,
                ProfilePicture = fileName,
                RegistrationDate = DateTime.UtcNow
            };


            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVolunteer), new { id = volunteer.Id }, volunteer);
        }

        // PUT: api/Volunteers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVolunteer(Guid id, UpdateVolunteerDto updateDto)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound();
            }

            volunteer.FirstName = updateDto.FirstName;
            volunteer.LastName = updateDto.LastName;
            volunteer.Email = updateDto.Email;
            volunteer.PhoneNumber = updateDto.PhoneNumber;
            volunteer.Address = updateDto.Address;
            volunteer.Skills = updateDto.Skills;
            volunteer.Availability = updateDto.Availability;
            volunteer.IsActive = updateDto.IsActive;
            volunteer.EmergencyContact = updateDto.EmergencyContact;
            volunteer.EmergencyPhone = updateDto.EmergencyPhone;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VolunteerExists(id))
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

        // DELETE: api/Volunteers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVolunteer(Guid id)
        {
            var volunteer = await _context.Volunteers.FindAsync(id);
            if (volunteer == null)
            {
                return NotFound();
            }

            // Soft delete - just deactivate
            volunteer.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VolunteerExists(Guid id)
        {
            return _context.Volunteers.Any(e => e.Id == id);
        }

        
    }
}
