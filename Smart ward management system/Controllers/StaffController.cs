using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Smart_ward_management_system.DTOs.Staff;
using Smart_ward_management_system.Services.Staff;

namespace Smart_ward_management_system.Controllers
{
    [ApiController]
    [Route("api/staff")]
    [Authorize(Roles = "admin")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? role, [FromQuery] string? wardNumber, [FromQuery] string? search)
        {
            var staff = await _staffService.GetAllAsync(role, wardNumber, search);
            return Ok(staff);
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var staff = await _staffService.GetByIdAsync(id);
            return staff is null ? NotFound() : Ok(staff);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStaffDto dto)
        {
            var adminUserId = GetCurrentUserId();

            try
            {
                var (staff, credentials) = await _staffService.CreateAsync(dto, adminUserId);
                return CreatedAtAction(nameof(GetById), new { id = staff.UserId }, new { staff, credentials });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffDto dto)
        {
            try
            {
                var updated = await _staffService.UpdateAsync(id, dto);
                return updated ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPatch("{id:guid}/status")]
        public async Task<IActionResult> SetStatus(Guid id, [FromBody] string accountStatus)
        {
            var updated = await _staffService.SetAccountStatusAsync(id, accountStatus);
            return updated ? NoContent() : NotFound();
        }

        [HttpPost("{id:guid}/reset-password")]
        public async Task<IActionResult> ResetPassword(Guid id)
        {
            var credentials = await _staffService.ResetPasswordAsync(id);
            return credentials is null ? NotFound() : Ok(credentials);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var deleted = await _staffService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }

        // Adjust this to however your auth setup exposes the logged-in admin's UserId claim
        private Guid GetCurrentUserId()
        {
            var claim = User.FindFirst("UserId")?.Value ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
        }
    }
}