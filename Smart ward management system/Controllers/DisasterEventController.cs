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

        //[Route("GetVolunteers")]
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<VolunteerDto>>> GetVolunteers()
        //{
        //    var volunteers = await _context.Volunteers
        //        .Include(v => v.Assignments)
        //        .Select(v => new VolunteerDto
        //        {
        //            Id = v.Id,
        //            FirstName = v.FirstName,
        //            LastName = v.LastName,
        //            Email = v.Email,
        //            PhoneNumber = v.PhoneNumber,
        //            Address = v.Address,
        //            DateOfBirth = v.DateOfBirth,
        //            Skills = v.Skills,
        //            Availability = v.Availability,
        //            IsActive = v.IsActive,
        //            RegistrationDate = v.RegistrationDate,
        //            EmergencyContact = v.EmergencyContact,
        //            EmergencyPhone = v.EmergencyPhone,
        //            ProfilePicture = v.ProfilePicture,
        //            ActiveAssignments = v.Assignments
        //                .Count(a => a.Status == "Assigned" || a.Status == "InProgress")
        //        })
        //        .ToListAsync();

        //    return Ok(volunteers);
        //}

        //// GET: api/Volunteers/{id}
        //[Route("GetVolunteer")]
        //[HttpGet("{id}")]
        //public async Task<ActionResult<VolunteerDto>> GetVolunteer(Guid id)
        //{
        //    var v = await _context.Volunteers
        //        .Include(v => v.Assignments)
        //        .FirstOrDefaultAsync(v => v.Id == id);

        //    if (v == null) return NotFound();

        //    return Ok(new VolunteerDto
        //    {
        //        Id = v.Id,
        //        FirstName = v.FirstName,
        //        LastName = v.LastName,
        //        Email = v.Email,
        //        PhoneNumber = v.PhoneNumber,
        //        Address = v.Address,
        //        DateOfBirth = v.DateOfBirth,
        //        Skills = v.Skills,
        //        Availability = v.Availability,
        //        IsActive = v.IsActive,
        //        RegistrationDate = v.RegistrationDate,
        //        EmergencyContact = v.EmergencyContact,
        //        EmergencyPhone = v.EmergencyPhone,
        //        ProfilePicture = v.ProfilePicture,
        //        ActiveAssignments = v.Assignments
        //            .Count(a => a.Status == "Assigned" || a.Status == "InProgress")
        //    });
        //}

        //// POST: api/Volunteers
        //[Route("AddVolunteer")]
        //[HttpPost]
        //public async Task<ActionResult<VolunteerDto>> CreateVolunteer([FromForm] CreateVolunteerDto dto)
        //{
        //    // Duplicate email check
        //    if (await _context.Volunteers.AnyAsync(v => v.Email == dto.Email))
        //        return Conflict(new { message = "A volunteer with this email already exists." });

        //    // Duplicate phone check
        //    if (await _context.Volunteers.AnyAsync(v => v.PhoneNumber == dto.PhoneNumber))
        //        return Conflict(new { message = "A volunteer with this phone number already exists." });

        //    // Handle profile picture upload
        //    string? picturePath = null;
        //    if (dto.ProfilePicture != null && dto.ProfilePicture.Length > 0)
        //    {
        //        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "volunteers");
        //        Directory.CreateDirectory(uploadsFolder);
        //        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(dto.ProfilePicture.FileName)}";
        //        var filePath = Path.Combine(uploadsFolder, fileName);
        //        using var stream = new FileStream(filePath, FileMode.Create);
        //        await dto.ProfilePicture.CopyToAsync(stream);
        //        picturePath = $"/uploads/volunteers/{fileName}";
        //    }

        //    var volunteer = new Volunteer
        //    {
        //        FirstName = dto.FirstName,
        //        LastName = dto.LastName,
        //        Email = dto.Email,
        //        PhoneNumber = dto.PhoneNumber,
        //        Address = dto.Address,
        //        DateOfBirth = dto.DateOfBirth,
        //        Skills = dto.Skills,
        //        Availability = dto.Availability,
        //        EmergencyContact = dto.EmergencyContact,
        //        EmergencyPhone = dto.EmergencyPhone,
        //        ProfilePicture = picturePath,
        //        IsActive = true,
        //        RegistrationDate = DateTime.UtcNow
        //    };

        //    _context.Volunteers.Add(volunteer);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetVolunteer), new { id = volunteer.Id }, new VolunteerDto
        //    {
        //        Id = volunteer.Id,
        //        FirstName = volunteer.FirstName,
        //        LastName = volunteer.LastName,
        //        Email = volunteer.Email,
        //        PhoneNumber = volunteer.PhoneNumber,
        //        Address = volunteer.Address,
        //        DateOfBirth = volunteer.DateOfBirth,
        //        Skills = volunteer.Skills,
        //        Availability = volunteer.Availability,
        //        IsActive = volunteer.IsActive,
        //        RegistrationDate = volunteer.RegistrationDate,
        //        EmergencyContact = volunteer.EmergencyContact,
        //        EmergencyPhone = volunteer.EmergencyPhone,
        //        ProfilePicture = volunteer.ProfilePicture,
        //        ActiveAssignments = 0
        //    });
        //}

        //// PUT: api/Volunteers/{id}
        //[Route("UpdateVolunteer")]
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateVolunteer(Guid id, UpdateVolunteerDto dto)
        //{
        //    var volunteer = await _context.Volunteers.FindAsync(id);
        //    if (volunteer == null) return NotFound();

        //    if (await _context.Volunteers.AnyAsync(v => v.Email == dto.Email && v.Id != id))
        //        return Conflict(new { message = "A volunteer with this email already exists." });

        //    if (await _context.Volunteers.AnyAsync(v => v.PhoneNumber == dto.PhoneNumber && v.Id != id))
        //        return Conflict(new { message = "A volunteer with this phone number already exists." });

        //    volunteer.FirstName = dto.FirstName;
        //    volunteer.LastName = dto.LastName;
        //    volunteer.Email = dto.Email;
        //    volunteer.PhoneNumber = dto.PhoneNumber;
        //    volunteer.Address = dto.Address;
        //    volunteer.Skills = dto.Skills;
        //    volunteer.Availability = dto.Availability;
        //    volunteer.IsActive = dto.IsActive;
        //    volunteer.EmergencyContact = dto.EmergencyContact;
        //    volunteer.EmergencyPhone = dto.EmergencyPhone;

        //    await _context.SaveChangesAsync();
        //    return NoContent();
        //}

        //// DELETE: api/Volunteers/{id}
        //[Route("DeleteVolunteer")]
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteVolunteer(Guid id)
        //{
        //    var volunteer = await _context.Volunteers.FindAsync(id);
        //    if (volunteer == null) return NotFound();

        //    // Prevent delete if active assignments exist
        //    var hasActive = await _context.VolunteerAssignments
        //        .AnyAsync(va => va.VolunteerId == id &&
        //                        (va.Status == "Assigned" || va.Status == "InProgress"));
        //    if (hasActive)
        //        return BadRequest(new { message = "Cannot delete a volunteer with active assignments." });

        //    _context.Volunteers.Remove(volunteer);
        //    await _context.SaveChangesAsync();
        //    return NoContent();
        //}

        //// GET: api/Volunteers/{id}/assignments
        //[HttpGet("{id}/assignments")]
        //public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetVolunteerAssignments(Guid id)
        //{
        //    if (!await _context.Volunteers.AnyAsync(v => v.Id == id))
        //        return NotFound();

        //    var assignments = await _context.VolunteerAssignments
        //        .Include(va => va.Volunteer)
        //        .Include(va => va.DisasterEvent)
        //        .Where(va => va.VolunteerId == id)
        //        .Select(va => new VolunteerAssignmentDto
        //        {
        //            Id = va.Id,
        //            VolunteerId = va.VolunteerId,
        //            VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
        //            DisasterEventId = va.DisasterEventId,
        //            DisasterEventName = va.DisasterEvent.EventName,
        //            Role = va.Role,
        //            AssignedDate = va.AssignedDate,
        //            StartDate = va.StartDate,
        //            EndDate = va.EndDate,
        //            Status = va.Status,
        //            Notes = va.Notes
        //        })
        //        .ToListAsync();

        //    return Ok(assignments);
        //}

        //[Route("GetAssignments")]
        //[HttpGet]
        //public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetAssignments()
        //{
        //    var assignments = await _context.VolunteerAssignments
        //        .Include(va => va.Volunteer)
        //        .Include(va => va.DisasterEvent)
        //        .Select(va => new VolunteerAssignmentDto
        //        {
        //            Id = va.Id,
        //            VolunteerId = va.VolunteerId,
        //            VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
        //            DisasterEventId = va.DisasterEventId,
        //            DisasterEventName = va.DisasterEvent.EventName,
        //            Role = va.Role,
        //            AssignedDate = va.AssignedDate,
        //            StartDate = va.StartDate,
        //            EndDate = va.EndDate,
        //            Status = va.Status,
        //            Notes = va.Notes
        //        })
        //        .ToListAsync();

        //    return Ok(assignments);
        //}

        //// GET: api/VolunteerAssignments/event/{eventId}
        //[HttpGet("event/{eventId}")]
        //public async Task<ActionResult<IEnumerable<VolunteerAssignmentDto>>> GetByEvent(Guid eventId)
        //{
        //    if (!await _context.DisasterEvents.AnyAsync(e => e.Id == eventId))
        //        return NotFound(new { message = "Disaster event not found." });

        //    var assignments = await _context.VolunteerAssignments
        //        .Include(va => va.Volunteer)
        //        .Include(va => va.DisasterEvent)
        //        .Where(va => va.DisasterEventId == eventId)
        //        .Select(va => new VolunteerAssignmentDto
        //        {
        //            Id = va.Id,
        //            VolunteerId = va.VolunteerId,
        //            VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
        //            DisasterEventId = va.DisasterEventId,
        //            DisasterEventName = va.DisasterEvent.EventName,
        //            Role = va.Role,
        //            AssignedDate = va.AssignedDate,
        //            StartDate = va.StartDate,
        //            EndDate = va.EndDate,
        //            Status = va.Status,
        //            Notes = va.Notes
        //        })
        //        .ToListAsync();

        //    return Ok(assignments);
        //}

        //// GET: api/VolunteerAssignments/{id}
        //[Route("GetAssignment")]
        //[HttpGet("{id}")]
        //public async Task<ActionResult<VolunteerAssignmentDto>> GetAssignment(Guid id)
        //{
        //    var va = await _context.VolunteerAssignments
        //        .Include(va => va.Volunteer)
        //        .Include(va => va.DisasterEvent)
        //        .FirstOrDefaultAsync(va => va.Id == id);

        //    if (va == null) return NotFound();

        //    return Ok(new VolunteerAssignmentDto
        //    {
        //        Id = va.Id,
        //        VolunteerId = va.VolunteerId,
        //        VolunteerName = va.Volunteer.FirstName + " " + va.Volunteer.LastName,
        //        DisasterEventId = va.DisasterEventId,
        //        DisasterEventName = va.DisasterEvent.EventName,
        //        Role = va.Role,
        //        AssignedDate = va.AssignedDate,
        //        StartDate = va.StartDate,
        //        EndDate = va.EndDate,
        //        Status = va.Status,
        //        Notes = va.Notes
        //    });
        //}

        //// POST: api/VolunteerAssignments
        //// Used by admin/staff to assign an existing volunteer to an event
        //[Route("CreateAssignment")]
        //[HttpPost]
        //public async Task<ActionResult<VolunteerAssignmentDto>> CreateAssignment(CreateVolunteerAssignmentDto dto)
        //{
        //    var volunteer = await _context.Volunteers.FindAsync(dto.VolunteerId);
        //    if (volunteer == null)
        //        return NotFound(new { message = "Volunteer not found." });

        //    var disasterEvent = await _context.DisasterEvents.FindAsync(dto.DisasterEventId);
        //    if (disasterEvent == null)
        //        return NotFound(new { message = "Disaster event not found." });

        //    // Prevent duplicate active assignment
        //    var alreadyAssigned = await _context.VolunteerAssignments.AnyAsync(va =>
        //        va.VolunteerId == dto.VolunteerId &&
        //        va.DisasterEventId == dto.DisasterEventId &&
        //        va.Status != "Cancelled" &&
        //        va.Status != "Completed");

        //    if (alreadyAssigned)
        //        return Conflict(new { message = "This volunteer is already assigned to this event." });

        //    var assignment = new VolunteerAssignment
        //    {
        //        VolunteerId = dto.VolunteerId,
        //        DisasterEventId = dto.DisasterEventId,
        //        Role = dto.Role ?? "General Volunteer",
        //        AssignedDate = DateTime.UtcNow,
        //        StartDate = dto.StartDate,
        //        Status = "Assigned",
        //        Notes = dto.Notes
        //    };

        //    _context.VolunteerAssignments.Add(assignment);
        //    await _context.SaveChangesAsync();

        //    return CreatedAtAction(nameof(GetAssignment), new { id = assignment.Id }, new VolunteerAssignmentDto
        //    {
        //        Id = assignment.Id,
        //        VolunteerId = assignment.VolunteerId,
        //        VolunteerName = volunteer.FirstName + " " + volunteer.LastName,
        //        DisasterEventId = assignment.DisasterEventId,
        //        DisasterEventName = disasterEvent.EventName,
        //        Role = assignment.Role,
        //        AssignedDate = assignment.AssignedDate,
        //        StartDate = assignment.StartDate,
        //        EndDate = assignment.EndDate,
        //        Status = assignment.Status,
        //        Notes = assignment.Notes
        //    });
        //}

        //// POST: api/VolunteerAssignments/self-register
        //// Called by citizen "Assign Me As Volunteer" button — creates profile + assignment in one shot
        //[HttpPost("self-register")]
        //public async Task<ActionResult<SelfRegisterResponseDto>> SelfRegister(SelfRegisterVolunteerDto dto)
        //{
        //    var disasterEvent = await _context.DisasterEvents.FindAsync(dto.DisasterEventId);
        //    if (disasterEvent == null)
        //        return NotFound(new { message = "Disaster event not found." });

        //    // Reuse existing volunteer profile if email already registered
        //    var volunteer = await _context.Volunteers
        //        .FirstOrDefaultAsync(v => v.Email == dto.Email);

        //    if (volunteer == null)
        //    {
        //        if (await _context.Volunteers.AnyAsync(v => v.PhoneNumber == dto.PhoneNumber))
        //            return Conflict(new { message = "A volunteer with this phone number already exists." });

        //        volunteer = new Volunteer
        //        {
        //            FirstName = dto.FirstName,
        //            LastName = dto.LastName,
        //            Email = dto.Email,
        //            PhoneNumber = dto.PhoneNumber,
        //            Address = dto.Address,
        //            DateOfBirth = dto.DateOfBirth ?? DateTime.MinValue,
        //            Skills = dto.Skills,
        //            Availability = dto.Availability,
        //            EmergencyContact = dto.EmergencyContact,
        //            EmergencyPhone = dto.EmergencyPhone,
        //            IsActive = true,
        //            RegistrationDate = DateTime.UtcNow
        //        };

        //        _context.Volunteers.Add(volunteer);
        //        await _context.SaveChangesAsync();
        //    }

        //    // Prevent duplicate active assignment for same event
        //    var alreadyAssigned = await _context.VolunteerAssignments.AnyAsync(va =>
        //        va.VolunteerId == volunteer.Id &&
        //        va.DisasterEventId == dto.DisasterEventId &&
        //        va.Status != "Cancelled" &&
        //        va.Status != "Completed");

        //    if (alreadyAssigned)
        //        return Conflict(new { message = "You are already registered as a volunteer for this event." });

        //    var assignment = new VolunteerAssignment
        //    {
        //        VolunteerId = volunteer.Id,
        //        DisasterEventId = dto.DisasterEventId,
        //        Role = "General Volunteer",
        //        AssignedDate = DateTime.UtcNow,
        //        Status = "Assigned",
        //        Notes = dto.Notes
        //    };

        //    _context.VolunteerAssignments.Add(assignment);
        //    await _context.SaveChangesAsync();

        //    return Ok(new SelfRegisterResponseDto
        //    {
        //        VolunteerId = volunteer.Id,
        //        AssignmentId = assignment.Id,
        //        Message = $"You have been successfully registered as a volunteer for '{disasterEvent.EventName}'."
        //    });
        //}

        //// PUT: api/VolunteerAssignments/{id}
        //[HttpPut("{id}")]
        //public async Task<IActionResult> UpdateAssignment(Guid id, UpdateVolunteerAssignmentDto dto)
        //{
        //    var assignment = await _context.VolunteerAssignments.FindAsync(id);
        //    if (assignment == null) return NotFound();

        //    var validStatuses = new[] { "Assigned", "InProgress", "Completed", "Cancelled" };
        //    if (!validStatuses.Contains(dto.Status))
        //        return BadRequest(new { message = $"Invalid status. Allowed values: {string.Join(", ", validStatuses)}" });

        //    assignment.Role = dto.Role ?? assignment.Role;
        //    assignment.StartDate = dto.StartDate ?? assignment.StartDate;
        //    assignment.EndDate = dto.EndDate ?? assignment.EndDate;
        //    assignment.Status = dto.Status;
        //    assignment.Notes = dto.Notes ?? assignment.Notes;

        //    await _context.SaveChangesAsync();
        //    return NoContent();
        //}

        //// DELETE: api/VolunteerAssignments/{id}
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteAssignment(Guid id)
        //{
        //    var assignment = await _context.VolunteerAssignments.FindAsync(id);
        //    if (assignment == null) return NotFound();

        //    _context.VolunteerAssignments.Remove(assignment);
        //    await _context.SaveChangesAsync();
        //    return NoContent();
        //}
    

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
