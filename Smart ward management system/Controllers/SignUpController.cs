

using Domain.Enumerators;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
        public SignUpController(ApplicationDbContext db, DocumentService _docService)
        {
            this.db = db;
            this._docService = _docService;
        }

        private async Task SendEmailAsync(string toEmail, string otp)
        {
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
                <p>Thank you,<br/>Smart ward mangement system</p>
            </body>
        </html>",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }

        // GET: api/<RecruiterSignUpController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<RecruiterSignUpController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<RecruiterSignUpController>

        [HttpPost("Register")]
        public async Task<ActionResult> Register([FromForm] UserDTO request)
        {
            var Email = db.Users.FirstOrDefault(s => s.Email == request.Email);
            var UserDetails = db.Users.FirstOrDefault(s => s.Username == request.Username);
            if (Email != null)
            {
                return BadRequest(new { message = "Email Already exist" });
            }
            if (UserDetails != null)
            {
                return BadRequest(new { message = "Username Already exist" });
            }
            using var transaction = await db.Database.BeginTransactionAsync();
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
                user.VerificationStatus = VerificationStatusEnum.Pending;
                user.VerifiedBy = Guid.Empty;
                user.VerifiedAt = null;
                user.AccountStatus = "Active";
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                db.Users.Add(user);
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

                db.SaveChanges();
                await transaction.CommitAsync();
                return Ok(new { message = "Registered successfully. Please check your email for OTP to confirm your account.", userId = user.UserId });

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

            if (user.OtpExpiryTime < DateTime.UtcNow)
                return BadRequest(new { message = "OTP expired. Please request a new one." });

            if (user.OtpCode != req.OtpCode)
                return BadRequest(new { message = "Invalid OTP." });

            // OTP correct
            user.IsEmailConfirmed = true;
            user.OtpCode = null; // clear OTP
            user.OtpExpiryTime = null; // clear expiry

            await db.SaveChangesAsync();

            return Ok(new { message = "Email verified successfully!" });
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
