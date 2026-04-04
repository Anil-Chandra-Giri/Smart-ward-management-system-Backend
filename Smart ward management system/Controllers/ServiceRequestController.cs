using Domain.Enumerators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Enumerators;
using Smart_ward_management_system.Model.Services;
using Smart_ward_management_system.Model.Services.ProbableServices;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceRequestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ServiceRequestController(ApplicationDbContext context)
        {
            _context = context;
        }
        // GET: api/<ServiceRequestController>
        [Route("GetAllRequestedServicesOfUser")]
        [HttpGet]
        public async Task<IActionResult> GetAllServices([FromQuery] Guid userId)
        {
            if(userId==null)
            {
                return NotFound("UserId is required");
            }
            var services = await _context.ServiceRequests.Where(s => s.UserId == userId).ToListAsync();
            return Ok(services);
        }

        [Route("GetAllRequestedServices")]
        [HttpGet]
        public async Task<IActionResult> GetAllRequestedServices()
        {
            var services = await _context.ServiceRequests.ToListAsync();
            return Ok(services);
        }
        // GET api/<ServiceRequestController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ServiceRequestController>
        [HttpPost]
        public async Task<IActionResult> CreateRequest([FromBody] ServiceRequestDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
               

                Guid currentUserId = dto.UserId;
                ServiceEnum type = (ServiceEnum)dto.ServiceType;

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
                request.ApplicationNumber = $"REQ-{DateTime.Now.Ticks.ToString().Substring(10)}";
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

                return Ok(new { message = "Request filed successfully!", reference = request.ApplicationNumber });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", details = ex.Message });
            }
        }

        // PUT api/<ServiceRequestController>/5
        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateServiceStatusDTO model)
        {
            var serviceRequest = await _context.ServiceRequests
                .FirstOrDefaultAsync(x => x.ServiceRequestId == model.Id);

            if (serviceRequest == null)
            {
                return NotFound("Service request not found");
            }

            serviceRequest.Status = model.Status;
            serviceRequest.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Service status updated successfully"
            });
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
