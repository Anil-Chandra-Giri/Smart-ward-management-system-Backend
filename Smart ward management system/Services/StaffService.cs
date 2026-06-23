using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Staff;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Services;

namespace Smart_ward_management_system.Services.Staff
{
    public class StaffService : IStaffService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILoggingService _logger;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public StaffService(ApplicationDbContext context, ILoggingService logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<StaffListDto>> GetAllAsync(string? role, string? wardNumber, string? search)
        {
            // Manage Staff only ever shows non-citizen accounts.
            var query = _context.Users.Where(u => u.Role != "Citizen");

            if (!string.IsNullOrWhiteSpace(role))
                query = query.Where(u => u.Role == role);

            if (!string.IsNullOrWhiteSpace(wardNumber))
                query = query.Where(u => u.WardNumber == wardNumber);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FullNameEnglish.ToLower().Contains(term) ||
                    u.Username.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    (u.EmployeeId != null && u.EmployeeId.ToLower().Contains(term)));
            }

            return await query
                .OrderBy(u => u.FullNameEnglish)
                .Select(u => MapToListDto(u))
                .ToListAsync();
        }

        public async Task<StaffListDto?> GetByIdAsync(Guid userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.Role != "Citizen");

            return user is null ? null : MapToListDto(user);
        }

        public async Task<(StaffListDto staff, StaffCredentialsDto credentials)> CreateAsync(CreateStaffDto dto, Guid adminUserId)
        {
            if (!AssignableStaffRoles.All.Contains(dto.Role))
                throw new InvalidOperationException($"Role must be one of: {string.Join(", ", AssignableStaffRoles.All)}.");

            var usernameTaken = await _context.Users.AnyAsync(u => u.Username == dto.Username);
            if (usernameTaken)
                throw new InvalidOperationException("Username is already taken.");

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailTaken)
                throw new InvalidOperationException("Email is already registered.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                FullNameEnglish = dto.FullNameEnglish,
                FullNameNepali = dto.FullNameNepali,
                PhoneNumber = dto.PhoneNumber,
                Role = dto.Role,
                EmployeeId = dto.EmployeeId,
                Department = dto.Department,
                Designation = dto.Designation,
                WardNumber = dto.WardNumber,
                Municipality = dto.Municipality,
                District = dto.District,
                Province = dto.Province,
                AccountStatus = "Active",
                // Admin-created accounts are pre-confirmed — no OTP step needed.
                IsEmailConfirmed = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow

                // NOTE: IsVerified / VerificationStatus / VerifiedBy on your User model
                // look like they belong to the citizen KYC flow (citizenship doc review).
                // Set them here too if staff accounts should also pass through that,
                // e.g.: IsVerified = true, VerificationStatus = VerificationStatusEnum.<YourApprovedValue>,
                // VerifiedBy = adminUserId, VerifiedAt = DateTime.UtcNow
            };

            var temporaryPassword = GenerateTemporaryPassword();
            user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var credentials = new StaffCredentialsDto
            {
                Username = user.Username,
                TemporaryPassword = temporaryPassword
            };

            await _logger.LogChangeAsync(
                $"Created staff account '{user.Username}' ({user.Role}, Ward {user.WardNumber})",
                LogCategory.UserManagement,
                "Staff",
                user.UserId,
                before: (StaffSnapshotDto?)null,
                after: ToSnapshot(user)
            );

            return (MapToListDto(user), credentials);
        }

        public async Task<bool> UpdateAsync(Guid userId, UpdateStaffDto dto)
        {
            if (!AssignableStaffRoles.All.Contains(dto.Role))
                throw new InvalidOperationException($"Role must be one of: {string.Join(", ", AssignableStaffRoles.All)}.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.Role != "Citizen");
            if (user is null) return false;

            var emailTaken = await _context.Users.AnyAsync(u => u.Email == dto.Email && u.UserId != userId);
            if (emailTaken)
                throw new InvalidOperationException("Email is already registered to another account.");

            var before = ToSnapshot(user);
            var statusChanged = user.AccountStatus != dto.AccountStatus;

            user.FullNameEnglish = dto.FullNameEnglish;
            user.FullNameNepali = dto.FullNameNepali;
            user.Email = dto.Email;
            user.PhoneNumber = dto.PhoneNumber;
            user.Role = dto.Role;
            user.EmployeeId = dto.EmployeeId;
            user.Department = dto.Department;
            user.Designation = dto.Designation;
            user.WardNumber = dto.WardNumber;
            user.Municipality = dto.Municipality;
            user.District = dto.District;
            user.Province = dto.Province;
            user.AccountStatus = dto.AccountStatus;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _logger.LogChangeAsync(
                statusChanged
                    ? $"Updated staff account '{user.Username}' and changed status to {user.AccountStatus}"
                    : $"Updated staff account '{user.Username}'",
                LogCategory.UserManagement,
                "Staff",
                user.UserId,
                before: before,
                after: ToSnapshot(user)
            );

            return true;
        }

        public async Task<bool> DeleteAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.Role != "Citizen");
            if (user is null) return false;

            var before = ToSnapshot(user);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await _logger.LogChangeAsync(
                $"Deleted staff account '{user.Username}' ({user.Role}, Ward {user.WardNumber})",
                LogCategory.UserManagement,
                "Staff",
                userId,
                before: before,
                after: (StaffSnapshotDto?)null
            );

            return true;
        }

        public async Task<StaffCredentialsDto?> ResetPasswordAsync(Guid userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.Role != "Citizen");
            if (user is null) return null;

            var temporaryPassword = GenerateTemporaryPassword();
            user.PasswordHash = _passwordHasher.HashPassword(user, temporaryPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _logger.LogInfoAsync(
                $"Reset password for staff account '{user.Username}'",
                LogCategory.UserManagement,
                new { TargetUserId = user.UserId, TargetUsername = user.Username }
            );

            return new StaffCredentialsDto
            {
                Username = user.Username,
                TemporaryPassword = temporaryPassword
            };
        }

        public async Task<bool> SetAccountStatusAsync(Guid userId, string accountStatus)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.Role != "Citizen");
            if (user is null) return false;

            var before = ToSnapshot(user);
            var previousStatus = user.AccountStatus;

            user.AccountStatus = accountStatus;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            await _logger.LogChangeAsync(
                $"Changed staff account '{user.Username}' status from {previousStatus} to {accountStatus}",
                LogCategory.UserManagement,
                "Staff",
                user.UserId,
                before: before,
                after: ToSnapshot(user)
            );

            return true;
        }

        private static StaffSnapshotDto ToSnapshot(User u) => new()
        {
            UserId = u.UserId,
            Username = u.Username,
            Email = u.Email,
            FullNameEnglish = u.FullNameEnglish,
            FullNameNepali = u.FullNameNepali,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            EmployeeId = u.EmployeeId,
            Department = u.Department,
            Designation = u.Designation,
            WardNumber = u.WardNumber,
            Municipality = u.Municipality,
            District = u.District,
            Province = u.Province,
            AccountStatus = u.AccountStatus
        };

        private static StaffListDto MapToListDto(User u) => new()
        {
            UserId = u.UserId,
            Username = u.Username,
            FullNameEnglish = u.FullNameEnglish,
            FullNameNepali = u.FullNameNepali,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            EmployeeId = u.EmployeeId,
            Department = u.Department,
            Designation = u.Designation,
            WardNumber = u.WardNumber,
            Municipality = u.Municipality,
            AccountStatus = u.AccountStatus,
            CreatedAt = u.CreatedAt,
            LastLoginAt = u.LastLoginAt
        };

        // Random is fine here since this is a one-time, must-be-changed temp password —
        // swap for RandomNumberGenerator if your security review wants crypto-strength generation.
        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
            var random = new Random();
            return new string(Enumerable.Range(0, 10).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}