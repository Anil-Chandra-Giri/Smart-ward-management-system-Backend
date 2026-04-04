

using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Identity;
using System.Net;
using System.Net.Mail;
using System.Threading;
using static System.Net.WebRequestMethods;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SignUpController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly DocumentService _docService;
        private readonly IConfiguration _configuration;
        public SignUpController(ApplicationDbContext db, DocumentService _docService, IConfiguration _configuration)
        {
            this.db = db;
            this._docService = _docService;
            this._configuration = _configuration;
        }

        private string GenerateOTP()
        {
            // Generate a 6-digit OTP
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task SendEmailAsync(string toEmail, string otp, string fullName)
        {
            try
            {
                // It's better to store email credentials in configuration
                var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpServer"])
                {
                    Port = int.Parse(_configuration["EmailSettings:Port"]),
                    Credentials = new NetworkCredential(
                        _configuration["EmailSettings:Username"],
                        _configuration["EmailSettings:Password"]),
                    EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSSL"]),
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:FromEmail"],
                           _configuration["EmailSettings:FromName"]),
                    Subject = "Verify Your Email - Smart Ward Management System",
                    Body = $@"
        <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; }}
                    .content {{ padding: 30px; background-color: #f9f9f9; }}
                    .otp-code {{ font-size: 32px; font-weight: bold; color: #4CAF50; text-align: center; 
                                 padding: 20px; letter-spacing: 5px; }}
                    .footer {{ text-align: center; padding: 20px; color: #666; font-size: 12px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Email Verification</h2>
                    </div>
                    <div class='content'>
                        <p>Hello <strong>{fullName}</strong>,</p>
                        <p>Thank you for registering with Smart Ward Management System. Please use the following OTP code to verify your email address:</p>
                        <div class='otp-code'>{otp}</div>
                        <p>This code is valid for <strong>10 minutes</strong>.</p>
                        <p>If you didn't request this verification, please ignore this email.</p>
                        <p>For security reasons, never share this OTP with anyone.</p>
                    </div>
                    <div class='footer'>
                        <p>© {DateTime.UtcNow.Year} Smart Ward Management System. All rights reserved.</p>
                    </div>
                </div>
            </body>
        </html>",
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log the exception
                throw new Exception($"Failed to send email: {ex.Message}");
            }
        }

        // GET: api/<RecruiterSignUpController>
        [HttpGet("GetUserProfile/{userId}")]
        public async Task<ActionResult> GetUserProfile(Guid userId)
        {
            var user = await db.Users
                .Where(u => u.UserId == userId)
                .Select(u => new
                {
                    u.UserId,
                    u.Username,
                    u.FullNameEnglish,
                    u.Email,
                    u.ProfilePicturePath,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }

        [HttpGet("GetProfilePicture/{userId}")]
        public async Task<IActionResult> GetProfilePicture(Guid userId)
        {
            try
            {
                var user = await db.Users.FindAsync(userId);
                if (user?.ProfilePicturePath == null)
                {
                    return NotFound(new { message = "Profile picture not found" });
                }

                // Get the full path
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), user.ProfilePicturePath.TrimStart('/'));

                if (!System.IO.File.Exists(imagePath))
                {
                    return NotFound(new { message = "Image file not found" });
                }

                // Determine content type based on file extension
                var fileExtension = Path.GetExtension(imagePath).ToLower();
                var contentType = fileExtension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                var image = System.IO.File.OpenRead(imagePath);
                return File(image, contentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving profile picture: {ex.Message}" });
            }
        }

        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromForm] UserDTO request)
        {
            // Check for existing email and username
            var existingEmail = await db.Users.FirstOrDefaultAsync(s => s.Email == request.Email);
            var existingUsername = await db.Users.FirstOrDefaultAsync(s => s.Username == request.Username);

            if (existingEmail != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }
            if (existingUsername != null)
            {
                return BadRequest(new { message = "Username already exists" });
            }

            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var user = new User();
                var hashedPassword = new PasswordHasher<User>().HashPassword(user, request.PasswordHash);

                user.UserId = Guid.NewGuid();
                user.FullNameNepali = request.FullNameNepali;
                user.FullNameEnglish = request.FullNameEnglish;
                user.Gender = request.Gender;
                user.DateOfBirth = request.DateOfBirth;
                user.CitizenshipNumber = request.CitizenshipNumber;
                user.CitizenshipIssuedDistrict = request.CitizenshipIssuedDistrict;
                user.CitizenshipIssuedDate = request.CitizenshipIssuedDate;
                user.NationalIdNumber = request.NationalIdNumber;
                user.PhoneNumber = request.PhoneNumber;
                user.Email = request.Email;
                user.Username = request.Username;
                user.PasswordHash = hashedPassword;
                user.PermanentAddress = request.PermanentAddress;
                user.TemporaryAddress = request.TemporaryAddress;
                user.WardNumber = request.WardNumber;
                user.Municipality = request.Municipality;
                user.District = request.District;
                user.Province = request.Province;
                user.Role = request.Role;
                user.IsVerified = false;
                user.IsEmailConfirmed = false; // Set to false initially
                user.VerificationStatus = VerificationStatusEnum.Pending;
                user.VerifiedBy = null;
                user.VerifiedAt = null;
                user.AccountStatus = "Pending Verification";
                if (request.LivePhoto != null)
                {
                    var profilePicPath = await FileHelper.SaveFileAsync(request.LivePhoto, "ProfilePictures");
                    user.ProfilePicturePath = profilePicPath;
                }
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                // Generate and store OTP
                string otpCode = GenerateOTP();
                user.OtpCode = otpCode;
                user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
                user.OtpAttempts = 0;
                user.LastOtpRequestTime = DateTime.UtcNow;

                db.Users.Add(user);

                // Handle document uploads
                var documentTypes = new[]
                {
                    (File: request.CitizenshipFront, Type: "CitizenshipFront"),
                    (File: request.CitizenshipBack, Type: "CitizenshipBack"),
                    (File: request.LivePhoto, Type: "LivePhoto")
                };

                foreach (var doc in documentTypes)
                {
                    if (doc.File != null)
                    {
                        var path = await FileHelper.SaveFileAsync(doc.File, "IdentityDocs");
                        string docNumber = await _docService.GenerateDocumentNumber(request.WardNumber, "REG");
                        var document = new Document
                        {
                            DocumentId = Guid.NewGuid(),
                            ReferenceId = user.UserId,
                            ReferenceType = "User",
                            DocumentType = doc.Type,
                            FilePath = path,
                            IssuedBy = request.CitizenshipIssuedDistrict,
                            IssuedDate = request.CitizenshipIssuedDate,
                            IsVerified = false,
                            CreatedOn = DateTime.UtcNow,
                            DocumentNumber = docNumber
                        };
                        db.Documents.Add(document);
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Send OTP email
                try
                {
                    await SendEmailAsync(user.Email, otpCode, user.FullNameEnglish);
                }
                catch (Exception ex)
                {
                    // Log email failure but don't rollback transaction
                    // User can request OTP again via resend endpoint
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }

                return Ok(new
                {
                    message = "Registration successful. Please check your email for OTP verification.",
                    userId = user.UserId,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { message = "Registration failed", error = ex.Message });
            }
        }


        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] OtpVerificationDTO req)
        {
            var user = await db.Users.FindAsync(req.UserId);

            if (user == null)
                return NotFound(new { message = "User not found" });

            if (user.IsEmailConfirmed)
                return BadRequest(new { message = "Email already confirmed." });

            // Check OTP attempts
            if (user.OtpAttempts >= 3)
                return BadRequest(new { message = "Too many failed attempts. Please request a new OTP." });

            if (user.OtpExpiryTime == null || user.OtpExpiryTime < DateTime.UtcNow)
                return BadRequest(new { message = "OTP expired. Please request a new one." });

            if (user.OtpCode != req.OtpCode)
            {
                user.OtpAttempts++;
                await db.SaveChangesAsync();

                int remainingAttempts = 3 - user.OtpAttempts;
                return BadRequest(new { message = $"Invalid OTP. {remainingAttempts} attempts remaining." });
            }

            user.IsEmailConfirmed = true;
            user.AccountStatus = "Active";
            user.OtpCode = null;
            user.OtpExpiryTime = null;
            user.OtpAttempts = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Email verified successfully! Your account is now active.",
                userId = user.UserId
            });
        }

        [HttpPost("ResendOTP")]
        public async Task<IActionResult> ResendOTP([FromBody] ResendOtpDTO req)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);

            if (user == null)
            {
                return Ok(new { message = "If your email is registered, you will receive an OTP." });
            }

            if (user.IsEmailConfirmed)
                return BadRequest(new { message = "Email already confirmed." });

            if (user.LastOtpRequestTime != null &&
                user.LastOtpRequestTime.Value.AddMinutes(2) > DateTime.UtcNow)
            {
                var waitTime = user.LastOtpRequestTime.Value.AddMinutes(2) - DateTime.UtcNow;
                return BadRequest(new { message = $"Please wait {waitTime.Seconds} seconds before requesting another OTP." });
            }

            string newOtp = GenerateOTP();
            user.OtpCode = newOtp;
            user.OtpExpiryTime = DateTime.UtcNow.AddMinutes(10);
            user.OtpAttempts = 0;
            user.LastOtpRequestTime = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            try
            {
                await SendEmailAsync(user.Email, newOtp, user.FullNameEnglish);
                return Ok(new { message = "New OTP has been sent to your email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to send OTP. Please try again." });
            }
        }

        // PUT api/<RecruiterSignUpController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<RecruiterSignUpController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
