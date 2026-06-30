using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Filters;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Services.Complaints;
using Smart_ward_management_system.Services;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly DocumentService _docService;
        private readonly ILoggingService _logger;

        public ComplaintController(ApplicationDbContext context, DocumentService docService, ILoggingService logger)
        {
            db = context;
            _docService = docService;
            _logger = logger;
        }

        // ── Writes — logging unchanged ──────────────────────────────────────

        [HttpPost("RegisterComplaint")]
        [RequireVerifiedCitizen]
        public async Task<ActionResult> RegisterComplaint([FromForm] ComplaintDTO dto)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Starting complaint registration for citizen: {dto.CitizenUserId}",
                    LogCategory.Grievance,
                    new
                    {
                        CorrelationId = correlationId,
                        CitizenUserId = dto.CitizenUserId,
                        Category = dto.Category,
                        Priority = dto.Priority,
                        WardNumber = dto.WardNumber
                    });

                if (string.IsNullOrEmpty(dto.ComplaintDetails))
                {
                    await _logger.LogWarningAsync($"Complaint registration failed: Required fields missing for citizen {dto.CitizenUserId}",
                        LogCategory.Grievance,
                        new { CorrelationId = correlationId, CitizenUserId = dto.CitizenUserId });
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

                    await _logger.LogInfoAsync($"Complaint created in database with ID: {complaint.ComplaintId}",
                        LogCategory.Grievance,
                        new
                        {
                            CorrelationId = correlationId,
                            ComplaintId = complaint.ComplaintId,
                            CitizenUserId = dto.CitizenUserId
                        });

                    string documentPath = null;
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
                        documentPath = document.FilePath;

                        await _logger.LogInfoAsync($"Complaint image uploaded for complaint {complaint.ComplaintId}",
                            LogCategory.Grievance,
                            new
                            {
                                CorrelationId = correlationId,
                                ComplaintId = complaint.ComplaintId,
                                DocumentNumber = docNumber
                            });
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _logger.LogComplaintAsync(complaint.ComplaintId, "filed", "Pending");

                    await _logger.LogCitizenActionAsync(
                        dto.CitizenUserId.ToString(),
                        $"Filed complaint: {dto.Category} - Priority: {dto.Priority}",
                        "Complaint Registration"
                    );

                    await _logger.LogInfoAsync($"Complaint registered successfully for citizen {dto.CitizenUserId} with ID: {complaint.ComplaintId}",
                        LogCategory.Grievance,
                        new
                        {
                            CorrelationId = correlationId,
                            ComplaintId = complaint.ComplaintId,
                            CitizenUserId = dto.CitizenUserId,
                            Category = dto.Category,
                            Priority = dto.Priority,
                            HasImage = documentPath != null
                        });

                    return Ok(new
                    {
                        message = "Complaint registered successfully.",
                        complaintId = complaint.ComplaintId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logger.LogErrorAsync($"Database error during complaint registration for citizen {dto.CitizenUserId}",
                        ex,
                        LogCategory.Grievance,
                        new
                        {
                            CorrelationId = correlationId,
                            CitizenUserId = dto.CitizenUserId,
                            ErrorMessage = ex.Message
                        });
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Unexpected error during complaint registration for citizen {dto.CitizenUserId}",
                    ex,
                    LogCategory.Grievance,
                    new
                    {
                        CorrelationId = correlationId,
                        CitizenUserId = dto.CitizenUserId,
                        ComplaintDetails = dto.ComplaintDetails?.Substring(0, Math.Min(100, dto.ComplaintDetails?.Length ?? 0))
                    });
                return StatusCode(500, new { message = "An error occurred while registering the complaint.", error = ex.Message });
            }
        }

        // ── Reads — Info-level "Fetching/Retrieved" noise removed ──────────
        // Errors and not-found cases still log; a clean read needs no audit entry.

        [HttpGet("Complaints")]
        public async Task<ActionResult<IEnumerable<Complaint>>> GetComplaints(Guid citizernUserId)
        {
            try
            {
                var complaints = await db.Complaints
                    .Where(s => s.CitizenUserId == citizernUserId)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return Ok(complaints);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching complaints for citizen {citizernUserId}",
                    ex,
                    LogCategory.Grievance,
                    new { CitizenUserId = citizernUserId });
                return StatusCode(500, new { message = "An error occurred while fetching complaints." });
            }
        }

        [Route("GetAllComplaints")]
        [HttpGet]
        public async Task<IActionResult> GetAllComplaints()
        {
            try
            {
                var complaints = await db.Complaints
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                return Ok(complaints);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching all complaints", ex, LogCategory.Grievance);
                return StatusCode(500, new { message = "An error occurred while fetching complaints." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Complaint>> GetComplaintById(Guid id)
        {
            try
            {
                var complaint = await db.Complaints
                    .FirstOrDefaultAsync(c => c.ComplaintId == id);

                if (complaint == null)
                {
                    await _logger.LogWarningAsync($"Complaint not found with ID: {id}",
                        LogCategory.Grievance,
                        new { ComplaintId = id });
                    return NotFound(new { message = "Complaint not found." });
                }

                return Ok(complaint);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching complaint by ID: {id}",
                    ex,
                    LogCategory.Grievance,
                    new { ComplaintId = id });
                return StatusCode(500, new { message = "An error occurred while fetching the complaint." });
            }
        }

        [HttpGet("GetByStatus")]
        public async Task<IActionResult> GetByStatus([FromQuery] string status)
        {
            try
            {
                var complaints = await db.Complaints
                    .Where(c => c.Status == status)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(complaints);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching complaints by status: {status}",
                    ex,
                    LogCategory.Grievance,
                    new { Status = status });
                return StatusCode(500, new { message = "An error occurred while fetching complaints." });
            }
        }

        [HttpGet("GetByWard")]
        public async Task<IActionResult> GetByWard([FromQuery] string wardNumber)
        {
            try
            {
                var complaints = await db.Complaints
                    .Where(c => c.WardNumber == wardNumber)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(complaints);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching complaints for ward: {wardNumber}",
                    ex,
                    LogCategory.Grievance,
                    new { WardNumber = wardNumber });
                return StatusCode(500, new { message = "An error occurred while fetching complaints." });
            }
        }

        [HttpGet("GetByPriority")]
        public async Task<IActionResult> GetByPriority([FromQuery] string priority)
        {
            try
            {
                var complaints = await db.Complaints
                    .Where(c => c.Priority == priority)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                return Ok(complaints);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching complaints by priority: {priority}",
                    ex,
                    LogCategory.Grievance,
                    new { Priority = priority });
                return StatusCode(500, new { message = "An error occurred while fetching complaints." });
            }
        }

        [HttpGet("GetStatistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var totalComplaints = await db.Complaints.CountAsync();
                var pendingComplaints = await db.Complaints.CountAsync(c => c.Status == "Pending");
                var inProgressComplaints = await db.Complaints.CountAsync(c => c.Status == "InProgress");
                var resolvedComplaints = await db.Complaints.CountAsync(c => c.Status == "Resolved");
                var rejectedComplaints = await db.Complaints.CountAsync(c => c.Status == "Rejected");

                var complaintsByWard = await db.Complaints
                    .GroupBy(c => c.WardNumber)
                    .Select(g => new { Ward = g.Key, Count = g.Count() })
                    .ToListAsync();

                var complaintsByCategory = await db.Complaints
                    .GroupBy(c => c.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync();

                var statistics = new
                {
                    Total = totalComplaints,
                    Pending = pendingComplaints,
                    InProgress = inProgressComplaints,
                    Resolved = resolvedComplaints,
                    Rejected = rejectedComplaints,
                    ByWard = complaintsByWard,
                    ByCategory = complaintsByCategory,
                    ResolutionRate = totalComplaints > 0
                        ? Math.Round((double)resolvedComplaints / totalComplaints * 100, 2)
                        : 0
                };

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching complaint statistics", ex, LogCategory.Grievance);
                return StatusCode(500, new { message = "An error occurred while fetching statistics." });
            }
        }

        // ── Writes — logging unchanged ──────────────────────────────────────

        [HttpPut("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] ComplaintStatusDto model)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Updating complaint status for ID: {model.Id}",
                    LogCategory.Grievance,
                    new { CorrelationId = correlationId, ComplaintId = model.Id, NewStatus = model.Status });

                var complaintRequest = await db.Complaints
                    .FirstOrDefaultAsync(x => x.ComplaintId == model.Id);

                if (complaintRequest == null)
                {
                    await _logger.LogWarningAsync($"Complaint not found for status update: {model.Id}",
                        LogCategory.Grievance,
                        new { CorrelationId = correlationId, ComplaintId = model.Id });
                    return NotFound("Complaint request not found");
                }

                var oldStatus = complaintRequest.Status;
                complaintRequest.Status = model.Status;

                await db.SaveChangesAsync();

                await _logger.LogComplaintAsync(
                    complaintRequest.ComplaintId,
                    $"status changed from {oldStatus} to {model.Status}",
                    model.Status
                );

                if (db.StatusHistories != null)
                {
                    var statusHistory = new StatusHistory
                    {
                        StatusHistoryId = Guid.NewGuid(),
                        EntityId = complaintRequest.ComplaintId,
                        EntityType = "Complaint",
                        OldStatus = oldStatus,
                        NewStatus = model.Status,
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = GetCurrentUserId()
                    };
                    db.StatusHistories.Add(statusHistory);
                    await db.SaveChangesAsync();
                }

                await _logger.LogInfoAsync($"Complaint status updated successfully for ID: {model.Id}",
                    LogCategory.Grievance,
                    new
                    {
                        CorrelationId = correlationId,
                        ComplaintId = model.Id,
                        OldStatus = oldStatus,
                        NewStatus = model.Status
                    });

                return Ok(new
                {
                    message = "Complaint status updated successfully",
                    oldStatus = oldStatus,
                    newStatus = model.Status
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating complaint status for ID: {model.Id}",
                    ex,
                    LogCategory.Grievance,
                    new { CorrelationId = correlationId, ComplaintId = model.Id });
                return StatusCode(500, new { message = "An error occurred while updating the status." });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComplaint(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogWarningAsync($"Attempting to delete complaint: {id}",
                    LogCategory.Grievance,
                    new { CorrelationId = correlationId, ComplaintId = id });

                var complaint = await db.Complaints.FindAsync(id);
                if (complaint == null)
                {
                    await _logger.LogWarningAsync($"Complaint not found for deletion: {id}",
                        LogCategory.Grievance,
                        new { CorrelationId = correlationId, ComplaintId = id });
                    return NotFound(new { message = "Complaint not found." });
                }

                var documents = db.Documents.Where(d => d.ReferenceId == id && d.ReferenceType == "Complaint");
                db.Documents.RemoveRange(documents);

                db.Complaints.Remove(complaint);
                await db.SaveChangesAsync();

                await _logger.LogWarningAsync($"Complaint deleted: {id}",
                    LogCategory.Grievance,
                    new
                    {
                        CorrelationId = correlationId,
                        ComplaintId = id,
                        CitizenUserId = complaint.CitizenUserId,
                        Category = complaint.Category
                    });

                return Ok(new { message = "Complaint deleted successfully" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error deleting complaint: {id}",
                    ex,
                    LogCategory.Grievance,
                    new { CorrelationId = correlationId, ComplaintId = id });
                return StatusCode(500, new { message = "An error occurred while deleting the complaint." });
            }
        }

        private Guid? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                return userId;
            }
            return null;
        }


        // ── Reads — Unresolved/Overdue complaints alert ─────────────────────
        [HttpGet("GetOverdueComplaints")]
        public async Task<IActionResult> GetOverdueComplaints([FromQuery] int days = 7)
        {
            try
            {
                var thresholdDate = DateTime.UtcNow.AddDays(-days);

                var overdueComplaints = await db.Complaints
                    .Where(c => c.Status != "Resolved"
                             && c.Status != "Rejected"
                             && c.CreatedAt <= thresholdDate)
                    .OrderBy(c => c.CreatedAt) // oldest/most overdue first
                    .ToListAsync();

                var result = overdueComplaints.Select(c => new
                {
                    c.ComplaintId,
                    c.CitizenUserId,
                    c.Category,
                    c.Priority,
                    c.Status,
                    c.WardNumber,
                    c.Municipality,
                    c.CreatedAt,
                    DaysPending = (int)(DateTime.UtcNow - c.CreatedAt).TotalDays
                }).ToList();

                var summary = new
                {
                    ThresholdDays = days,
                    TotalOverdue = result.Count,
                    ByPriority = result.GroupBy(c => c.Priority)
                                        .Select(g => new { Priority = g.Key, Count = g.Count() })
                                        .ToList(),
                    ByWard = result.GroupBy(c => c.WardNumber)
                                   .Select(g => new { Ward = g.Key, Count = g.Count() })
                                   .ToList(),
                    Complaints = result
                };

                if (result.Any())
                {
                    await _logger.LogWarningAsync(
                        $"Overdue complaints alert: {result.Count} complaint(s) unresolved for >= {days} days",
                        LogCategory.Grievance,
                        new { ThresholdDays = days, TotalOverdue = result.Count });
                }

                return Ok(summary);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching overdue complaints (threshold: {days} days)",
                    ex,
                    LogCategory.Grievance,
                    new { ThresholdDays = days });
                return StatusCode(500, new { message = "An error occurred while fetching overdue complaints." });
            }
        }
    }
}