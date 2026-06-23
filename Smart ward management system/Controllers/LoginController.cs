using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Services;
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
        private readonly ILoggingService _logger;

        public LoginController(ApplicationDbContext db, IConfiguration _config, ILoggingService logger)
        {
            this.db = db;
            this._config = _config;
            this._logger = logger;
        }

        // POST api/Login
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

                var user = db.Users.FirstOrDefault(s => s.Username == req.Username);

                if (user == null)
                {
                    await _logger.LogWarningAsync($"Login failed: Username not found - {req.Username}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, Username = req.Username, Reason = "Invalid Username" });
                    return BadRequest(new { message = "Invalid Username" });
                }

                var passwordVerificationResult = new PasswordHasher<User>()
                    .VerifyHashedPassword(user, user.PasswordHash, req.Password);

                if (passwordVerificationResult == PasswordVerificationResult.Failed)
                {
                    await _logger.LogWarningAsync($"Login failed: Invalid password for user {user.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = user.UserId, Reason = "Invalid Password" });
                    return BadRequest(new { message = "Invalid Password" });
                }

                if (!user.IsEmailConfirmed)
                {
                    await _logger.LogWarningAsync($"Login failed: Email not verified for user {user.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = user.UserId, Reason = "Email Not Verified" });
                    return BadRequest(new { message = "Please Verify Email First" });
                }

                if (user.AccountStatus != "Active")
                {
                    await _logger.LogWarningAsync($"Login failed: Account not active for user {user.UserId}",
                        LogCategory.UserManagement,
                        new { CorrelationId = correlationId, UserId = user.UserId, AccountStatus = user.AccountStatus });
                    return BadRequest(new { message = $"Your account is {user.AccountStatus}. Please contact administrator." });
                }

                // Build JWT — IsFirstLogin claim lets the guard catch bypasses
                var authClaims = new List<Claim>
                {
                    new Claim("UserName", user.Username),
                    new Claim("UserId", user.UserId.ToString()),
                    new Claim("Role", user.Role),
                    new Claim("WardNumber", user.WardNumber?.ToString() ?? ""),
                    new Claim("Email", user.Email ?? ""),
                    new Claim("IsFirstLogin", user.IsFirstLogin.ToString()),
                    new Claim("IsVerified", user.IsVerified.ToString())
                };

                var secret = _config["JWT:Key"];
                if (string.IsNullOrEmpty(secret))
                {
                    await _logger.LogErrorAsync("JWT Secret not configured", null, LogCategory.System,
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

                await _logger.LogInfoAsync($"Login successful for user {user.UserId}",
                    LogCategory.UserManagement,
                    new
                    {
                        CorrelationId = correlationId,
                        UserId = user.UserId,
                        Role = user.Role,
                        IsFirstLogin = user.IsFirstLogin,
                        TokenExpiry = token.ValidTo
                    });

                await _logger.LogCitizenActionAsync(user.UserId.ToString(), "Logged in successfully", "Authentication");

                return Ok(new
                {
                    token = tokenString,
                    expiration = token.ValidTo,
                    isFirstLogin = user.IsFirstLogin, // ← NEW: frontend reads this
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
                    ex, LogCategory.UserManagement,
                    new { CorrelationId = correlationId, Username = req.Username });

                return StatusCode(500, new { message = "An error occurred during login. Please try again later." });
            }
        }

        // POST api/Login/ChangePassword  — requires valid JWT token
        [HttpPost("ChangePassword")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO req)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { message = "Invalid token." });

                var user = await db.Users.FindAsync(Guid.Parse(userIdClaim));
                if (user == null)
                    return NotFound(new { message = "User not found." });

                // Verify the temporary password
                var verify = new PasswordHasher<User>()
                    .VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword);

                if (verify == PasswordVerificationResult.Failed)
                {
                    await _logger.LogWarningAsync($"ChangePassword failed: wrong current password for user {user.UserId}",
                        LogCategory.UserManagement, new { CorrelationId = correlationId, UserId = user.UserId });
                    return BadRequest(new { message = "Current password is incorrect." });
                }

                // Hash new password and clear first-login flag
                user.PasswordHash = new PasswordHasher<User>().HashPassword(user, req.NewPassword);
                user.IsFirstLogin = false;
                await db.SaveChangesAsync();

                await _logger.LogInfoAsync($"Password changed successfully for user {user.UserId}",
                    LogCategory.UserManagement, new { CorrelationId = correlationId, UserId = user.UserId });

                return Ok(new { message = "Password changed successfully." });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error during ChangePassword", ex, LogCategory.UserManagement,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = ex.Message, detail = ex.InnerException?.Message }); // ← temp: return real error
            }
        }

        // POST api/Login/Logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                var userIdClaim = User.FindFirst("UserId")?.Value;
                var usernameClaim = User.FindFirst("UserName")?.Value;

                await _logger.LogInfoAsync("User logout", LogCategory.UserManagement,
                    new { CorrelationId = correlationId, UserId = userIdClaim, Username = usernameClaim, LogoutTime = DateTime.UtcNow });

                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error during logout", ex, LogCategory.UserManagement,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred during logout." });
            }
        }

        // POST api/Login/RefreshToken
        [HttpPost("RefreshToken")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDTO refreshTokenRequest)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync("Token refresh requested", LogCategory.UserManagement,
                    new { CorrelationId = correlationId });

                // TODO: implement proper refresh token logic
                return Ok(new { message = "Token refresh endpoint" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync("Error during token refresh", ex, LogCategory.UserManagement,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred during token refresh." });
            }
        }
    }
}