using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Services.Complaints;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly DocumentService _docService;

        public ComplaintController(ApplicationDbContext context, DocumentService docService)
        {
            db = context;
            _docService = docService;
        }


        [HttpPost("RegisterComplaint")]
        public async Task<ActionResult> RegisterComplaint([FromForm] ComplaintDTO dto)
        {
            if (string.IsNullOrEmpty(dto.ComplaintDetails))
            {
                return BadRequest(new { message = "Required fields are missing." });
            }

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var complaint = new Complaint
                {
                    ComplaintId = Guid.NewGuid(),
                    CitizenUserId = dto.CitizenUserId,
                    Category = dto.Category,
                    ComplaintDetails = dto.ComplaintDetails,
                    Priority = dto.Priority,
                    WardNumber = dto.WardNumber,
                    Municipality = dto.Municipality,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    CreatedAt = DateTime.UtcNow,
                    Status = "Pending"
                };

                db.Complaints.Add(complaint);
                await db.SaveChangesAsync(); 

                if (dto.ComplaintImage != null)
                {
                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ComplaintImage.FileName);

                    
                    var path = await FileHelper.SaveFileAsync(dto.ComplaintImage, "ComplaintImages");
                    string docNumber = await _docService.GenerateDocumentNumber(dto.WardNumber, "CMP");

                    var document = new Document
                    {
                        DocumentId = Guid.NewGuid(),
                        ReferenceId = complaint.ComplaintId,  
                        ReferenceType = "Complaint", 
                        DocumentType = "ComplaintImage", 
                        FilePath = path,
                        IssuedBy = dto.Municipality,
                        IssuedDate = DateTime.UtcNow,
                        IsVerified = false,
                        CreatedOn = DateTime.UtcNow,
                        DocumentNumber = docNumber 
                    };

                    db.Documents.Add(document);
                complaint.ImageUrl = document.FilePath;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();  

                return Ok(new { message = "Complaint registered successfully.", complaintId = complaint.ComplaintId });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred while registering the complaint.", error = ex.Message });
            }
        }

       
        [HttpGet("Complaints")]
        public ActionResult<IEnumerable<Complaint>> GetComplaints(Guid citizernUserId)
        {
            var complaints = db.Complaints.Where(s => s.CitizenUserId == citizernUserId).ToList();
            return Ok(complaints);
        }

        [Route("GetAllComplaints")]
        [HttpGet]
        public async Task<IActionResult> GetAllComplaints()
        {
            var complaints = await db.Complaints.ToListAsync();
            return Ok(complaints);
        }
        [HttpGet("{id}")]
        public ActionResult<Complaint> GetComplaintById(Guid id)
        {
            var complaint = db.Complaints.FirstOrDefault(c => c.ComplaintId == id);
            if (complaint == null)
            {
                return NotFound(new { message = "Complaint not found." });
            }
            return Ok(complaint);
        }

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] ComplaintStatusDto model)
        {
            var complaintRequest = await db.Complaints
                .FirstOrDefaultAsync(x => x.ComplaintId == model.Id);

            if (complaintRequest == null)
            {
                return NotFound("Complaint request not found");
            }

            complaintRequest.Status = model.Status;

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Complaint status updated successfully"
            });
        }
    }
}