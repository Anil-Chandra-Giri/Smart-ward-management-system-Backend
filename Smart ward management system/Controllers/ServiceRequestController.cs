using Domain.Enumerators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Enumerators;
using Smart_ward_management_system.Model.Services;
using Smart_ward_management_system.Model.Services.Complaints;
using Smart_ward_management_system.Model.Services.ProbableServices;
using Smart_ward_management_system.Services; // Add this

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoggingService _logger; // Add logging service

        public ServiceRequestController(ApplicationDbContext context, ILoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/<ServiceRequestController>/GetAllRequestedServicesOfUser
        [Route("GetAllRequestedServicesOfUser")]
        [HttpGet]
        public async Task<IActionResult> GetAllServices([FromQuery] Guid userId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching all service requests for user: {userId}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, UserId = userId });

                if (userId == null)
                {
                    await _logger.LogWarningAsync($"GetAllServices called with null userId",
                        LogCategory.ServiceRequests,
                        new { CorrelationId = correlationId });
                    return NotFound("UserId is required");
                }

                var services = await _context.ServiceRequests
                    .Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {services.Count} service requests for user: {userId}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, UserId = userId, Count = services.Count });

                return Ok(services);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching service requests for user: {userId}",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, UserId = userId });
                return StatusCode(500, new { message = "An error occurred while fetching service requests." });
            }
        }

        [Route("GetAllRequestedServices")]
        [HttpGet]
        public async Task<IActionResult> GetAllRequestedServices()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching all service requests",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId });

                var services = await _context.ServiceRequests
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {services.Count} total service requests",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, Count = services.Count });

                return Ok(services);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching all service requests",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching service requests." });
            }
        }

        // GET api/<ServiceRequestController>/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceRequestById(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching service request by ID: {id}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = id });

                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(s => s.ServiceRequestId == id);

                if (serviceRequest == null)
                {
                    await _logger.LogWarningAsync($"Service request not found with ID: {id}",
                        LogCategory.ServiceRequests,
                        new { CorrelationId = correlationId, ServiceRequestId = id });
                    return NotFound(new { message = "Service request not found" });
                }

                await _logger.LogInfoAsync($"Retrieved service request {id} of type: {serviceRequest.ServiceType}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = id, ServiceType = serviceRequest.ServiceType });

                return Ok(serviceRequest);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching service request by ID: {id}",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = id });
                return StatusCode(500, new { message = "An error occurred while fetching the service request." });
            }
        }

        // POST api/<ServiceRequestController>
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] ServiceRequestDTO dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            if (!ModelState.IsValid)
            {
                await _logger.LogWarningAsync($"Invalid model state for service request creation",
                    LogCategory.ServiceRequests,
                    new
                    {
                        CorrelationId = correlationId,
                        Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                return BadRequest(ModelState);
            }

            try
            {
                ServiceEnum type = (ServiceEnum)dto.ServiceType;

                await _logger.LogInfoAsync($"Creating new service request of type: {type} for user: {dto.UserId}",
                    LogCategory.ServiceRequests,
                    new
                    {
                        CorrelationId = correlationId,
                        UserId = dto.UserId,
                        ServiceType = type.ToString(),
                        PriorityLevel = dto.PriorityLevel,
                        RequestedWard = dto.RequestedWard
                    });

                Guid currentUserId = dto.UserId;

                // 2. Initialize the specific class based on Enum
                ServiceRequest request = type switch
                {
                    ServiceEnum.BirthCertificate => new BirthCertificateRequest
                    {
                        ChildFullName = dto.ChildFullName,
                        DateOfBirth = dto.DateOfBirth ?? DateTime.Now,
                        Gender = dto.Gender,
                        PlaceOfBirth = dto.PlaceOfBirth,
                        FatherFullName = dto.FatherFullName,
                        MotherFullName = dto.MotherFullName,
                        GrandfatherFullName = dto.GrandfatherFullName,
                        PermanentAddress = dto.PermanentAddress
                    },
                    ServiceEnum.DeathCertificate => new DeathCertificateRequest
                    {
                        DeceasedFullName = dto.DeceasedFullName,
                        DateOfDeath = dto.DateOfDeath ?? DateTime.Now,
                        PlaceOfDeath = dto.PlaceOfDeath,
                        CauseOfDeath = dto.CauseOfDeath,
                        RelationshipToApplicant = dto.RelationshipToApplicant,
                        CitizenshipNoOfDeceased = dto.CitizenshipNoOfDeceased
                    },
                    ServiceEnum.RecommendationLetter => new RecommendationLetterRequest
                    {
                        LetterCategory = dto.LetterCategory,
                        RecipientOrganization = dto.RecipientOrganization
                    },
                    ServiceEnum.PropertyDocument => new PropertyDocumentRequest
                    {
                        PlotNumber = dto.PlotNumber,
                        SheetNumber = dto.SheetNumber,
                        TotalArea = dto.TotalArea ?? 0,
                        PropertyType = dto.PropertyType,
                        CurrentOwnerName = dto.CurrentOwnerName,
                        LandRevenueReceiptNumber = dto.LandRevenueReceiptNumber
                    },
                    ServiceEnum.MarriageRegistration => new MarriageRegistrationRequest
                    {
                        GroomFullName = dto.GroomFullName,
                        BrideFullName = dto.BrideFullName,
                        MarriageDate = dto.MarriageDate ?? DateTime.Now,
                        MarriageVenue = dto.MarriageVenue,
                        GroomCitizenshipNo = dto.GroomCitizenshipNo,
                        BrideCitizenshipNo = dto.BrideCitizenshipNo,
                        WitnessName = dto.WitnessName
                    },
                    ServiceEnum.MigrationCertificate => new MigrationCertificateRequest
                    {
                        MigrationType = dto.MigrationType,
                        OriginAddress = dto.OriginAddress,
                        DestinationAddress = dto.DestinationAddress,
                        TotalFamilyMembersMoving = dto.TotalFamilyMembersMoving ?? 1,
                        ReasonForMigration = dto.ReasonForMigration
                    },
                    ServiceEnum.AddressVerification => new AddressVerificationRequest
                    {
                        HouseNumber = dto.HouseNumber,
                        StreetName = dto.StreetName,
                        YearsOfStay = dto.YearsOfStay ?? 0
                    },
                    _ => new ServiceRequest()
                };

                // 3. Map Base Properties (Common to all)
                request.ServiceRequestId = Guid.NewGuid();
                request.UserId = currentUserId;
                request.ServiceType = type;
                request.ApplicationNumber = GenerateApplicationNumber(type);
                request.Purpose = dto.Purpose;
                request.Description = dto.Description;
                request.RequestedWard = dto.RequestedWard;
                request.RequestedMunicipality = dto.RequestedMunicipality;
                request.PriorityLevel = (PriorityLevelEnum)dto.PriorityLevel;
                request.Status = ApprovalStatusEnum.Pending;
                request.SubmissionMode = dto.SubmissionMode;
                request.PaymentStatus = "Unpaid";
                request.Remarks = dto.Remarks;
                request.CreatedAt = DateTime.Now;
                request.UpdatedAt = DateTime.Now;

                // 4. Save
                _context.ServiceRequests.Add(request);
                await _context.SaveChangesAsync();

                // Log the successful creation
                await _logger.LogServiceRequestAsync(
                    request.ServiceRequestId,
                    type.ToString(),
                    "created",
                    "Pending"
                );

                // Log citizen action
                await _logger.LogCitizenActionAsync(
                    currentUserId.ToString(),
                    $"Filed service request: {type} (Ref: {request.ApplicationNumber})",
                    "Service Request"
                );

                // Log certificate request if applicable
                if (type == ServiceEnum.BirthCertificate)
                {
                    await _logger.LogCertificateRequestAsync(
                        "Birth Certificate",
                        currentUserId.ToString(),
                        request.ApplicationNumber,
                        "submitted"
                    );
                }
                else if (type == ServiceEnum.DeathCertificate)
                {
                    await _logger.LogCertificateRequestAsync(
                        "Death Certificate",
                        currentUserId.ToString(),
                        request.ApplicationNumber,
                        "submitted"
                    );
                }

                await _logger.LogInfoAsync($"Service request created successfully with Application Number: {request.ApplicationNumber}",
                    LogCategory.ServiceRequests,
                    new
                    {
                        CorrelationId = correlationId,
                        ServiceRequestId = request.ServiceRequestId,
                        ApplicationNumber = request.ApplicationNumber,
                        ServiceType = type.ToString()
                    });

                return Ok(new
                {
                    message = "Request filed successfully!",
                    reference = request.ApplicationNumber,
                    requestId = request.ServiceRequestId
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error creating service request for user: {dto.UserId}",
                    ex,
                    LogCategory.ServiceRequests,
                    new
                    {
                        CorrelationId = correlationId,
                        UserId = dto.UserId,
                        ServiceType = dto.ServiceType,
                        ErrorMessage = ex.Message
                    });
                return StatusCode(500, new { message = "Server error", details = ex.Message });
            }
        }

        // PUT api/<ServiceRequestController>/update-status
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateServiceStatusDTO model)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Updating service request status for ID: {model.Id}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = model.Id, NewStatus = model.Status });

                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(x => x.ServiceRequestId == model.Id);

                if (serviceRequest == null)
                {
                    await _logger.LogWarningAsync($"Service request not found for status update: {model.Id}",
                        LogCategory.ServiceRequests,
                        new { CorrelationId = correlationId, ServiceRequestId = model.Id });
                    return NotFound(new { message = "Service request not found" });
                }

                var oldStatus = serviceRequest.Status;
                serviceRequest.Status = model.Status;
                serviceRequest.UpdatedAt = DateTime.Now;

                // Add to status history if you have a StatusHistory table
                var statusHistory = new StatusHistory
                {
                    StatusHistoryId = Guid.NewGuid(),
                    EntityId = serviceRequest.ServiceRequestId,
                    EntityType = "ServiceRequest",
                    OldStatus = oldStatus.ToString(),
                    NewStatus = model.Status.ToString(),
                    ChangedAt = DateTime.Now,
                    ChangedBy = GetCurrentUserId() // Implement this method
                };
                _context.StatusHistories.Add(statusHistory);

                await _context.SaveChangesAsync();

                // Log the status change
                await _logger.LogServiceRequestAsync(
                    serviceRequest.ServiceRequestId,
                    serviceRequest.ServiceType.ToString(),
                    $"status changed from {oldStatus} to {model.Status}",
                    model.Status.ToString()
                );

                await _logger.LogInfoAsync($"Service request status updated successfully for ID: {model.Id}",
                    LogCategory.ServiceRequests,
                    new
                    {
                        CorrelationId = correlationId,
                        ServiceRequestId = model.Id,
                        OldStatus = oldStatus.ToString(),
                        NewStatus = model.Status.ToString()
                    });

                return Ok(new
                {
                    message = "Service status updated successfully",
                    oldStatus = oldStatus.ToString(),
                    newStatus = model.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating service request status for ID: {model.Id}",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = model.Id });
                return StatusCode(500, new { message = "An error occurred while updating the status." });
            }
        }

        // GET: api/<ServiceRequestController>/GetByStatus
        [HttpGet("GetByStatus")]
        public async Task<IActionResult> GetByStatus([FromQuery] ApprovalStatusEnum status)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching service requests by status: {status}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, Status = status });

                var services = await _context.ServiceRequests
                    .Where(s => s.Status == status)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {services.Count} service requests with status: {status}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, Status = status, Count = services.Count });

                return Ok(services);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching service requests by status: {status}",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, Status = status });
                return StatusCode(500, new { message = "An error occurred while fetching service requests." });
            }
        }

        // GET: api/<ServiceRequestController>/GetByWard
        [HttpGet("GetByWard")]
        public async Task<IActionResult> GetByWard([FromQuery] string wardNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching service requests for ward: {wardNumber}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });

                var services = await _context.ServiceRequests
                    .Where(s => s.RequestedWard == wardNumber)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {services.Count} service requests for ward: {wardNumber}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, WardNumber = wardNumber, Count = services.Count });

                return Ok(services);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching service requests for ward: {wardNumber}",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });
                return StatusCode(500, new { message = "An error occurred while fetching service requests." });
            }
        }

        // PUT api/<ServiceRequestController>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRequest(Guid id, [FromBody] ServiceRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var existingRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id);

                if (existingRequest == null)
                {
                    return NotFound("Service request not found");
                }

                // Update base properties
                existingRequest.Purpose = dto.Purpose;
                existingRequest.Description = dto.Description;
                existingRequest.RequestedWard = dto.RequestedWard;
                existingRequest.RequestedMunicipality = dto.RequestedMunicipality;
                existingRequest.PriorityLevel = (PriorityLevelEnum)dto.PriorityLevel;
                existingRequest.Remarks = dto.Remarks;
                existingRequest.UpdatedAt = DateTime.Now;

                // Update service-specific properties based on type
                switch (existingRequest.ServiceType)
                {
                    case ServiceEnum.BirthCertificate:
                        if (existingRequest is BirthCertificateRequest birthRequest)
                        {
                            birthRequest.ChildFullName = dto.ChildFullName;
                            birthRequest.DateOfBirth = dto.DateOfBirth ?? DateTime.Now;
                            birthRequest.Gender = dto.Gender;
                            birthRequest.PlaceOfBirth = dto.PlaceOfBirth;
                            birthRequest.FatherFullName = dto.FatherFullName;
                            birthRequest.MotherFullName = dto.MotherFullName;
                            birthRequest.GrandfatherFullName = dto.GrandfatherFullName;
                            birthRequest.PermanentAddress = dto.PermanentAddress;
                        }
                        break;

                    case ServiceEnum.DeathCertificate:
                        if (existingRequest is DeathCertificateRequest deathRequest)
                        {
                            deathRequest.DeceasedFullName = dto.DeceasedFullName;
                            deathRequest.DateOfDeath = dto.DateOfDeath ?? DateTime.Now;
                            deathRequest.PlaceOfDeath = dto.PlaceOfDeath;
                            deathRequest.CauseOfDeath = dto.CauseOfDeath;
                            deathRequest.RelationshipToApplicant = dto.RelationshipToApplicant;
                            deathRequest.CitizenshipNoOfDeceased = dto.CitizenshipNoOfDeceased;
                        }
                        break;

                    case ServiceEnum.RecommendationLetter:
                        if (existingRequest is RecommendationLetterRequest recRequest)
                        {
                            recRequest.LetterCategory = dto.LetterCategory;
                            recRequest.RecipientOrganization = dto.RecipientOrganization;
                        }
                        break;

                    case ServiceEnum.PropertyDocument:
                        if (existingRequest is PropertyDocumentRequest propRequest)
                        {
                            propRequest.PlotNumber = dto.PlotNumber;
                            propRequest.SheetNumber = dto.SheetNumber;
                            propRequest.TotalArea = dto.TotalArea ?? 0;
                            propRequest.PropertyType = dto.PropertyType;
                            propRequest.CurrentOwnerName = dto.CurrentOwnerName;
                            propRequest.LandRevenueReceiptNumber = dto.LandRevenueReceiptNumber;
                        }
                        break;

                    case ServiceEnum.MarriageRegistration:
                        if (existingRequest is MarriageRegistrationRequest marriageRequest)
                        {
                            marriageRequest.GroomFullName = dto.GroomFullName;
                            marriageRequest.BrideFullName = dto.BrideFullName;
                            marriageRequest.MarriageDate = dto.MarriageDate ?? DateTime.Now;
                            marriageRequest.MarriageVenue = dto.MarriageVenue;
                            marriageRequest.GroomCitizenshipNo = dto.GroomCitizenshipNo;
                            marriageRequest.BrideCitizenshipNo = dto.BrideCitizenshipNo;
                            marriageRequest.WitnessName = dto.WitnessName;
                        }
                        break;

                    case ServiceEnum.MigrationCertificate:
                        if (existingRequest is MigrationCertificateRequest migrationRequest)
                        {
                            migrationRequest.MigrationType = dto.MigrationType;
                            migrationRequest.OriginAddress = dto.OriginAddress;
                            migrationRequest.DestinationAddress = dto.DestinationAddress;
                            migrationRequest.TotalFamilyMembersMoving = dto.TotalFamilyMembersMoving ?? 1;
                            migrationRequest.ReasonForMigration = dto.ReasonForMigration;
                        }
                        break;

                    case ServiceEnum.AddressVerification:
                        if (existingRequest is AddressVerificationRequest addressRequest)
                        {
                            addressRequest.HouseNumber = dto.HouseNumber;
                            addressRequest.StreetName = dto.StreetName;
                            addressRequest.YearsOfStay = dto.YearsOfStay ?? 0;
                        }
                        break;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Request updated successfully!", reference = existingRequest.ApplicationNumber });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", details = ex.Message });
            }
        }

        // DELETE api/<ServiceRequestController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogWarningAsync($"Attempting to delete service request: {id}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = id });

                var serviceRequest = await _context.ServiceRequests.FindAsync(id);
                if (serviceRequest == null)
                {
                    await _logger.LogWarningAsync($"Service request not found for deletion: {id}",
                        LogCategory.ServiceRequests,
                        new { CorrelationId = correlationId, ServiceRequestId = id });
                    return NotFound(new { message = "Service request not found" });
                }

                _context.ServiceRequests.Remove(serviceRequest);
                await _context.SaveChangesAsync();

                await _logger.LogWarningAsync($"Service request deleted: {id}",
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = id, ServiceType = serviceRequest.ServiceType });

                return Ok(new { message = "Service request deleted successfully" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deleting service request: {id}",
                    ex,
                    LogCategory.ServiceRequests,
                    new { CorrelationId = correlationId, ServiceRequestId = id });
                return StatusCode(500, new { message = "An error occurred while deleting the service request." });
            }
        }

        // Helper method to generate application number
        private string GenerateApplicationNumber(ServiceEnum serviceType)
        {
            var prefix = serviceType switch
            {
                ServiceEnum.BirthCertificate => "BIR",
                ServiceEnum.DeathCertificate => "DEA",
                ServiceEnum.RecommendationLetter => "REC",
                ServiceEnum.PropertyDocument => "PRO",
                ServiceEnum.MarriageRegistration => "MAR",
                ServiceEnum.MigrationCertificate => "MIG",
                ServiceEnum.AddressVerification => "ADD",
                _ => "SRV"
            };

            return $"{prefix}-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 6).ToUpper()}";
        }

        // Helper method to get current user ID
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }
            return null;
        }
        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> DeleteRequest(Guid id)
        {
            try
            {
                var serviceRequest = await _context.ServiceRequests
                    .FirstOrDefaultAsync(sr => sr.ServiceRequestId == id);

                if (serviceRequest == null)
                {
                    return NotFound("Service request not found");
                }

                _context.ServiceRequests.Remove(serviceRequest);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Request deleted successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", details = ex.Message });
            }
        }
    }
}