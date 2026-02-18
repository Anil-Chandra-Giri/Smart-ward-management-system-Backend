
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Model.Identity;
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

        public LoginController(ApplicationDbContext db, IConfiguration _config)
        {
            this.db = db;
            this._config = _config;
        }

        // GET: api/<LoginController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<LoginController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<LoginController>
        [HttpPost]
        public ActionResult<string> Login([FromBody] LoginDTO req)
        {
            var user = db.Users.FirstOrDefault(s => s.Username == req.Username);

            if (user == null)
            {
                return BadRequest(new { message = "Invalid Username" });
            }

            var passwordVerificationResult = new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, req.Password);
            if (passwordVerificationResult == PasswordVerificationResult.Failed)
            {
                return BadRequest(new { message = "Invalid Password" });
            }
            var isEmailVerified = user.IsEmailConfirmed;
            if(isEmailVerified==false)
            {
                return BadRequest(new { message = "Please Verify Email First" });
            }

            var userId = user.UserId;
            var authClaims = new List<Claim>
                 {
                     new Claim("UserName",user.Username),
                     new Claim("UserId",userId.ToString()),
                     new Claim("Role",user.Role)
                 };
            var secret = _config["JWT:Key"];
            if (string.IsNullOrEmpty(secret))
            {
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
            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo
            });
        }

        // PUT api/<LoginController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<LoginController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
