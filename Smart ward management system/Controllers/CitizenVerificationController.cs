using Domain.Enumerators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Services;
using System.Net;
using System.Net.Mail;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CitizenVerificationController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly ILoggingService _logger;
        private readonly IConfiguration _configuration;

        public CitizenVerificationController(ApplicationDbContext db, ILoggingService logger, IConfiguration configuration)
        {
            this.db = db;
            this._logger = logger;
            this._configuration = configuration;
        }

        // GET api/CitizenVerification/pending
        // Returns all citizens with VerificationStatus = Pending
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingCitizens()
        {
            try
            {
                var pendingCitizens = await db.Users
                    .Where(u => u.Role == "citizen" && u.VerificationStatus == VerificationStatusEnum.Pending && u.IsEmailConfirmed)
                    .OrderBy(u => u.CreatedAt)
                    .Select(u => new
                    {
                        userId = u.UserId,
                        fullNameEnglish = u.FullNameEnglish,
                        fullNameNepali = u.FullNameNepali,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber,
                        gender = u.Gender,
                        dateOfBirth = u.DateOfBirth,
                        citizenshipNumber = u.CitizenshipNumber,
                        citizenshipIssuedDistrict = u.CitizenshipIssuedDistrict,
                        citizenshipIssuedDate = u.CitizenshipIssuedDate,
                        nationalIdNumber = u.NationalIdNumber,
                        permanentAddress = u.PermanentAddress,
                        temporaryAddress = u.TemporaryAddress,
                        wardNumber = u.WardNumber,
                        municipality = u.Municipality,
                        district = u.District,
                        province = u.Province,
                        profilePicturePath = u.ProfilePicturePath,
                        verificationStatus = u.VerificationStatus.ToString(),
                        isVerified = u.IsVerified,
                        createdAt = u.CreatedAt,
                        // Include documents for this citizen
                        documents = db.Documents
                            .Where(d => d.ReferenceId == u.UserId && d.ReferenceType == "User")
                            .Select(d => new
                            {
                                documentId = d.DocumentId,
                                documentType = d.DocumentType,
                                filePath = d.FilePath,
                                isVerified = d.IsVerified,
                                documentNumber = d.DocumentNumber
                            }).ToList()
                    })
                    .ToListAsync();

                return Ok(pendingCitizens);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error fetching pending citizens", ex, LogCategory.UserManagement, null);
                return StatusCode(500, new { message = "Error fetching pending citizens." });
            }
        }

        // GET api/CitizenVerification/{id}
        // Returns a single citizen's full details
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCitizenDetail(Guid id)
        {
            try
            {
                var citizen = await db.Users
                    .Where(u => u.UserId == id)
                    .Select(u => new
                    {
                        userId = u.UserId,
                        fullNameEnglish = u.FullNameEnglish,
                        fullNameNepali = u.FullNameNepali,
                        email = u.Email,
                        phoneNumber = u.PhoneNumber,
                        gender = u.Gender,
                        dateOfBirth = u.DateOfBirth,
                        citizenshipNumber = u.CitizenshipNumber,
                        citizenshipIssuedDistrict = u.CitizenshipIssuedDistrict,
                        citizenshipIssuedDate = u.CitizenshipIssuedDate,
                        nationalIdNumber = u.NationalIdNumber,
                        permanentAddress = u.PermanentAddress,
                        temporaryAddress = u.TemporaryAddress,
                        wardNumber = u.WardNumber,
                        municipality = u.Municipality,
                        district = u.District,
                        province = u.Province,
                        profilePicturePath = u.ProfilePicturePath,
                        verificationStatus = u.VerificationStatus.ToString(),
                        isVerified = u.IsVerified,
                        createdAt = u.CreatedAt,
                        documents = db.Documents
                            .Where(d => d.ReferenceId == u.UserId && d.ReferenceType == "User")
                            .Select(d => new
                            {
                                documentId = d.DocumentId,
                                documentType = d.DocumentType,
                                filePath = d.FilePath,
                                isVerified = d.IsVerified,
                                documentNumber = d.DocumentNumber
                            }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (citizen == null)
                    return NotFound(new { message = "Citizen not found." });

                return Ok(citizen);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching citizen detail for {id}", ex, LogCategory.UserManagement, null);
                return StatusCode(500, new { message = "Error fetching citizen details." });
            }
        }

        // POST api/CitizenVerification/verify/{id}
        // Verifies a citizen and sends email notification
        [HttpPost("verify/{id}")]
        public async Task<IActionResult> VerifyCitizen(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var staffIdClaim = User.FindFirst("UserId")?.Value;

                var citizen = await db.Users.FindAsync(id);
                if (citizen == null)
                    return NotFound(new { message = "Citizen not found." });

                if (citizen.IsVerified)
                    return BadRequest(new { message = "Citizen is already verified." });

                // Set verification fields
                citizen.IsVerified = true;
                citizen.VerificationStatus = VerificationStatusEnum.Approved;
                citizen.VerifiedAt = DateTime.UtcNow;
                citizen.VerifiedBy = staffIdClaim != null ? Guid.Parse(staffIdClaim) : Guid.Empty;
                citizen.UpdatedAt = DateTime.UtcNow;

                // Also verify their documents
                var documents = db.Documents
                    .Where(d => d.ReferenceId == citizen.UserId && d.ReferenceType == "User");
                foreach (var doc in documents)
                {
                    doc.IsVerified = true;
                }

                await db.SaveChangesAsync();

                // Send email notification
                try
                {
                    await SendVerificationEmailAsync(citizen.Email, citizen.FullNameEnglish);
                }
                catch (Exception emailEx)
                {
                    // Don't fail the whole request if email fails
                    await _logger.LogErrorAsync($"Verification email failed for citizen {id}", emailEx, LogCategory.UserManagement, null);
                }

                await _logger.LogInfoAsync($"Citizen {id} verified by staff {staffIdClaim}",
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, CitizenId = id, VerifiedBy = staffIdClaim });

                return Ok(new { message = "Citizen verified successfully. Notification email sent." });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error verifying citizen {id}", ex, LogCategory.UserManagement,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "Error verifying citizen." });
            }
        }

        // POST api/CitizenVerification/reject/{id}
        // Rejects a citizen's verification with a reason
        [HttpPost("reject/{id}")]
        public async Task<IActionResult> RejectCitizen(Guid id, [FromBody] RejectCitizenDTO req)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var staffIdClaim = User.FindFirst("UserId")?.Value;

                var citizen = await db.Users.FindAsync(id);
                if (citizen == null)
                    return NotFound(new { message = "Citizen not found." });

                citizen.VerificationStatus = VerificationStatusEnum.Rejected;
                citizen.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();

                // Send rejection email
                try
                {
                    await SendRejectionEmailAsync(citizen.Email, citizen.FullNameEnglish, req.Reason);
                }
                catch (Exception emailEx)
                {
                    await _logger.LogErrorAsync($"Rejection email failed for citizen {id}", emailEx, LogCategory.UserManagement, null);
                }

                await _logger.LogInfoAsync($"Citizen {id} rejected by staff {staffIdClaim}",
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, CitizenId = id, Reason = req.Reason });

                return Ok(new { message = "Citizen verification rejected. Notification email sent." });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error rejecting citizen {id}", ex, LogCategory.UserManagement,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "Error rejecting citizen." });
            }
        }

        // ─── Email Helpers ────────────────────────────────────────────────────────

        private async Task SendVerificationEmailAsync(string toEmail, string fullName)
        {
            var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
            {
                Port = int.Parse(_configuration["EmailSettings:Port"]!),
                Credentials = new NetworkCredential(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]),
                EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSSL"]!),
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(
                    _configuration["EmailSettings:FromEmail"]!,
                    _configuration["EmailSettings:FromName"]),
                Subject = "Account Verified - Smart Ward Management System",
                Body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #00b4d8; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 30px; background-color: #f9f9f9; border-radius: 0 0 8px 8px; }}
        .badge {{ display: inline-block; background-color: #28a745; color: white; padding: 10px 24px;
                  border-radius: 20px; font-size: 16px; font-weight: bold; margin: 16px 0; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h2>✔ Account Verified</h2></div>
        <div class='content'>
            <p>Dear <strong>{fullName}</strong>,</p>
            <p>We are pleased to inform you that your account has been <strong>verified</strong> by our ward staff.</p>
            <div style='text-align:center'><span class='badge'>✔ Verified</span></div>
            <p>You can now fully access the Smart Ward Management System and:</p>
            <ul>
                <li>Submit complaints</li>
                <li>Request services</li>
                <li>Book appointments</li>
                <li>Vote in polls</li>
            </ul>
            <p>Thank you for registering with us.</p>
        </div>
        <div class='footer'>© {DateTime.UtcNow.Year} Smart Ward Management System. All rights reserved.</div>
    </div>
</body>
</html>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }

        private async Task SendRejectionEmailAsync(string toEmail, string fullName, string reason)
        {
            var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
            {
                Port = int.Parse(_configuration["EmailSettings:Port"]!),
                Credentials = new NetworkCredential(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]),
                EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSSL"]!),
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(
                    _configuration["EmailSettings:FromEmail"]!,
                    _configuration["EmailSettings:FromName"]),
                Subject = "Account Verification Update - Smart Ward Management System",
                Body = $@"
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ padding: 30px; background-color: #f9f9f9; border-radius: 0 0 8px 8px; }}
        .reason {{ background: #fff3cd; border-left: 4px solid #ffc107; padding: 12px 16px; margin: 16px 0; border-radius: 4px; }}
        .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'><h2>Verification Update</h2></div>
        <div class='content'>
            <p>Dear <strong>{fullName}</strong>,</p>
            <p>Unfortunately, your account verification could not be approved at this time.</p>
            <div class='reason'><strong>Reason:</strong> {reason}</div>
            <p>Please visit the ward office or re-register with the correct documents to try again.</p>
            <p>If you believe this is a mistake, please contact the ward office directly.</p>
        </div>
        <div class='footer'>© {DateTime.UtcNow.Year} Smart Ward Management System. All rights reserved.</div>
    </div>
</body>
</html>",
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}