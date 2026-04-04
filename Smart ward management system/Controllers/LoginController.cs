using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Services; // Add this
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ApplicationDbContext db;
        private readonly IConfiguration _config;
        private readonly ILoggingService _logger; // Add logging service

        public LoginController(ApplicationDbContext db, IConfiguration _config, ILoggingService logger)
        {
            this.db = db;
            this._config = _config;
            this._logger = logger;
        }

        // GET: api/<LoginController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            _logger.LogInfoAsync("LoginController GET endpoint called", LogCategory.System).Wait();
            return new string[] { "value1", "value2" };
        }

        // GET api/<LoginController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            _logger.LogInfoAsync($"LoginController GET by id {id} called", LogCategory.System).Wait();
            return "value";
        }

        // POST api/<LoginController>
        [HttpPost]
        public async Task<ActionResult<string>> Login([FromBody] LoginDTO req)
        {
            var correlationId = Guid.NewGuid().ToString();
            var loginTime = DateTime.UtcNow;

            try
            {
                await _logger.LogInfoAsync($"Login attempt for username: {req.Username}",
                    LogCategory.UserManagement,
                    new
                    {
                        CorrelationId = correlationId,
                        Username = req.Username,
                        LoginTime = loginTime,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    });

                // Find user by username
                var user = db.Users.FirstOrDefault(s => s.Username == req.Username);

                if (user == null)
                {
                    await _logger.LogWarningAsync($"Login failed: Username not found - {req.Username}",
                        LogCategory.UserManagement,
                        new
                        {
                            CorrelationId = correlationId,
                            Username = req.Username,
                            Reason = "Invalid Username"
                        });
                    return BadRequest(new { message = "Invalid Username" });
                }

                // Verify password
                var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, req.Password);
                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    await _logger.LogWarningAsync($"Login failed: Invalid password for user {user.UserId}",
                        LogCategory.UserManagement,
                        new
                        {
                            CorrelationId = correlationId,
                            UserId = user.UserId,
                            Username = req.Username,
                            Reason = "Invalid Password"
                        });
                    return BadRequest(new { message = "Invalid Password" });
                }

                // Check email verification
                var isEmailVerified = user.IsEmailConfirmed;
                if (isEmailVerified == false)
                {
                    await _logger.LogWarningAsync($"Login failed: Email not verified for user {user.UserId}",
                        LogCategory.UserManagement,
                        new
                        {
                            CorrelationId = correlationId,
                            UserId = user.UserId,
                            Username = req.Username,
                            Email = user.Email,
                            Reason = "Email Not Verified"
                        });
                    return BadRequest(new { message = "Please Verify Email First" });
                }

                // Check account status
                if (user.AccountStatus != "Active")
                {
                    await _logger.LogWarningAsync($"Login failed: Account not active for user {user.UserId}",
                        LogCategory.UserManagement,
                        new
                        {
                            CorrelationId = correlationId,
                            UserId = user.UserId,
                            Username = req.Username,
                            AccountStatus = user.AccountStatus,
                            Reason = "Account Inactive"
                        });
                    return BadRequest(new { message = $"Your account is {user.AccountStatus}. Please contact administrator." });
                }

                // Generate JWT token
                var userId = user.UserId;
                var authClaims = new List<Claim>
                {
                    new Claim("UserName", user.Username),
                    new Claim("UserId", userId.ToString()),
                    new Claim("Role", user.Role),
                    new Claim("WardNumber", user.WardNumber?.ToString() ?? ""),
                    new Claim("Email", user.Email ?? "")
                };

                var secret = _config["JWT:Key"];
                if (string.IsNullOrEmpty(secret))
                {
                    await _logger.LogErrorAsync($"JWT Secret not configured",
                        null,
                        LogCategory.System,
                        new { CorrelationId = correlationId });
                    return BadRequest(new { message = "JWT Secret is not configured properly." });
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
                var token = new JwtSecurityToken(
                    issuer: _config["Jwt:ValidIssuer"],
                    audience: _config["Jwt:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // Log successful login
                await _logger.LogInfoAsync($"Login successful for user {user.UserId}",
                    LogCategory.UserManagement,
                    new
                    {
                        CorrelationId = correlationId,
                        UserId = user.UserId,
                        Username = req.Username,
                        Role = user.Role,
                        WardNumber = user.WardNumber,
                        LoginTime = loginTime,
                        TokenExpiry = token.ValidTo
                    });

                // Log citizen action
                await _logger.LogCitizenActionAsync(
                    user.UserId.ToString(),
                    "Logged in successfully",
                    "Authentication"
                );

                // Update last login time (if you have this field in your User model)
                // user.LastLoginAt = DateTime.UtcNow;
                // await db.SaveChangesAsync();

                return Ok(new
                {
                    token = tokenString,
                    expiration = token.ValidTo,
                    user = new
                    {
                        userId = user.UserId,
                        username = user.Username,
                        email = user.Email,
                        role = user.Role,
                        wardNumber = user.WardNumber,
                        fullName = user.FullNameEnglish
                    }
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Unexpected error during login for username: {req.Username}",
                    ex,
                    LogCategory.UserManagement,
                    new
                    {
                        CorrelationId = correlationId,
                        Username = req.Username,
                        LoginTime = loginTime
                    });

                return StatusCode(500, new { message = "An error occurred during login. Please try again later." });
            }
        }

        // POST api/<LoginController>/logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                // Get user info from claims if available
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var usernameClaim = User.FindFirst("UserName")?.Value;

                await _logger.LogInfoAsync($"User logout",
                    LogCategory.UserManagement,
                    new
                    {
                        CorrelationId = correlationId,
                        UserId = userIdClaim,
                        Username = usernameClaim,
                        LogoutTime = DateTime.UtcNow
                    });

                // In a real implementation, you might want to:
                // 1. Blacklist the JWT token
                // 2. Clear server-side session
                // 3. Log the logout action

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error during logout",
                    ex,
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId });

                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        // POST api/<LoginController>/refresh-token
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO refreshTokenRequest)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Token refresh requested",
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId });

                // Validate and refresh token logic here
                // This is a placeholder - implement proper token refresh logic

                return Ok(new { message = "Token refresh endpoint" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error during token refresh",
                    ex,
                    LogCategory.UserManagement,
                    new { CorrelationId = correlationId });

                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }

        // PUT api/<LoginController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
            _logger.LogInfoAsync($"LoginController PUT called for id {id}", LogCategory.System).Wait();
        }

        // DELETE api/<LoginController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            _logger.LogWarningAsync($"LoginController DELETE called for id {id}", LogCategory.System).Wait();
        }
    }
}