using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Services; // Add this
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
        private readonly ILoggingService _logger; // Add logging service

        public SignUpController(ApplicationDbContext db, DocumentService _docService, ILoggingService logger)
        {
            this.db = db;
            this._docService = _docService;
            this._logger = logger;
        }

        private async Task SendEmailAsync(string toEmail, string otp)
        {
            try
            {
                await _logger.LogInfoAsync($"Sending OTP email to {toEmail}", LogCategory.CitizenServices);

                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("dineshjoshi0025@gmail.com", "jhzcuiigttvriohy"),
                    EnableSsl = true,
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreply@smartwardmanagementsystem.com"),
                    Subject = "Your One-Time Password (OTP) for Secure Login",
                    Body = $@"
        <html>
            <body>
                <p>Hello,</p>
                <p>Your OTP code is: <strong>{otp}</strong></p>
                <p>This code is valid for 10 minutes.</p>
                <p>If you did not request this, please ignore this email.</p>
                <br/>
                <p>Thank you,<br/>Smart ward management system</p>
            </body>
        </html>",
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);

                await _logger.LogInfoAsync($"OTP email sent successfully to {toEmail}", LogCategory.CitizenServices);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Failed to send OTP email to {toEmail}", ex, LogCategory.CitizenServices);
                throw;
            }
        }

        // GET: api/<SignUpController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            _logger.LogInfoAsync("SignUpController GET endpoint called", LogCategory.System).Wait();
            return new string[] { "value1", "value2" };
        }

        // GET api/<SignUpController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            _logger.LogInfoAsync($"SignUpController GET by id {id} called", LogCategory.System).Wait();
            return "value";
        }

        // POST api/<SignUpController>/Register
        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromForm] UserDTO request)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Registration started for user: {request.Username}, Email: {request.Email}",
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, Username = request.Username, Email = request.Email });

                // Check for existing email
                var existingEmail = db.Users.FirstOrDefault(s => s.Email == request.Email);
                if (existingEmail != null)
                {
                    await _logger.LogWarningAsync($"Registration failed: Email already exists - {request.Email}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, Email = request.Email });
                    return BadRequest(new { message = "Email Already exist" });
                }

                // Check for existing username
                var existingUser = db.Users.FirstOrDefault(s => s.Username == request.Username);
                if (existingUser != null)
                {
                    await _logger.LogWarningAsync($"Registration failed: Username already exists - {request.Username}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, Username = request.Username });
                    return BadRequest(new { message = "Username Already exist" });
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
                    user.IsEmailConfirmed = true;
                    user.VerificationStatus = VerificationStatusEnum.Pending;
                    user.VerifiedBy = Guid.Empty;
                    user.VerifiedAt = null;
                    user.AccountStatus = "Active";
                    user.CreatedAt = DateTime.UtcNow;
                    user.UpdatedAt = DateTime.UtcNow;

                    db.Users.Add(user);

                    await _logger.LogInfoAsync($"User created successfully with ID: {user.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = user.UserId, WardNumber = user.WardNumber });

                    // Process documents
                    var documentTypes = new[]
                    {
                        (File: request.CitizenshipFront, Type: "CitizenshipFront"),
                        (File: request.CitizenshipBack, Type: "CitizenshipBack"),
                        (File: request.LivePhoto, Type: "LivePhoto")
                    };

                    int documentCount = 0;
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
                            documentCount++;
                        }
                    }

                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await _logger.LogInfoAsync($"Registration completed successfully for user: {user.UserId}. Documents uploaded: {documentCount}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = user.UserId, DocumentsUploaded = documentCount });

                    // Log citizen action
                    await _logger.LogCitizenActionAsync(
                        user.UserId.ToString(),
                        "Registered as new user",
                        "User Registration"
                    );

                    return Ok(new
                    {
                        message = "Registered successfully. Please check your email for OTP to confirm your account.",
                        userId = user.UserId
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    await _logger.LogErrorAsync($"Registration failed during database operation",
                        ex,
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, Username = request.Username, Email = request.Email });
                    throw;
                }
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Unexpected error during registration for user: {request.Username}",
                    ex,
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, Username = request.Username });
                return StatusCode(500, new { message = "An error occurred during registration. Please try again." });
            }
        }

        [HttpPost("VerifyEmail")]
        public async Task<IActionResult> VerifyEmail([FromBody] OtpVerificationDTO req)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Email verification started for UserId: {req.UserId}",
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, UserId = req.UserId });

                var user = await db.Users.FindAsync(req.UserId);

                if (user == null)
                {
                    await _logger.LogWarningAsync($"Email verification failed: User not found - {req.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = req.UserId });
                    return NotFound(new { message = "User not found" });
                }

                if (user.IsEmailConfirmed)
                {
                    await _logger.LogWarningAsync($"Email verification failed: Email already confirmed for user {req.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = req.UserId });
                    return BadRequest(new { message = "Email already confirmed." });
                }

                if (user.OtpExpiryTime < DateTime.UtcNow)
                {
                    await _logger.LogWarningAsync($"Email verification failed: OTP expired for user {req.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = req.UserId, OtpExpiry = user.OtpExpiryTime });
                    return BadRequest(new { message = "OTP expired. Please request a new one." });
                }

                if (user.OtpCode != req.OtpCode)
                {
                    await _logger.LogWarningAsync($"Email verification failed: Invalid OTP for user {req.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = req.UserId, ProvidedOtp = req.OtpCode });
                    return BadRequest(new { message = "Invalid OTP." });
                }

                // OTP correct - verify email
                user.IsEmailConfirmed = true;
                user.OtpCode = null;
                user.OtpExpiryTime = null;

                await db.SaveChangesAsync();

                await _logger.LogInfoAsync($"Email verified successfully for user {req.UserId}",
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, UserId = req.UserId, Email = user.Email });

                // Log citizen action
                await _logger.LogCitizenActionAsync(
                    user.UserId.ToString(),
                    "Email verified successfully",
                    "Account Verification"
                );

                return Ok(new { message = "Email verified successfully!" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error during email verification for user {req.UserId}",
                    ex,
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId, UserId = req.UserId });
                return StatusCode(500, new { message = "An error occurred during email verification." });
            }
        }

        // PUT api/<SignUpController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
            _logger.LogInfoAsync($"SignUpController PUT called for id {id}", LogCategory.System).Wait();
        }

        // DELETE api/<SignUpController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _logger.LogWarningAsync($"SignUpController DELETE called for id {id}", LogCategory.System).Wait();
        }
    }
}