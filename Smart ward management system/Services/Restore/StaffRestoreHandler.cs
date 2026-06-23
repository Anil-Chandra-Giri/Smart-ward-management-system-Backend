using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.Staff;
using Smart_ward_management_system.Model.Identity;
using System.Text.Json;

namespace Smart_ward_management_system.Services.Restore
{
    public class StaffRestoreHandler : IEntityRestoreHandler
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public string EntityType => "Staff";

        public StaffRestoreHandler(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RestoreResult> RestoreAsync(Guid entityId, string beforeStateJson)
        {
            var snapshot = JsonSerializer.Deserialize<StaffSnapshotDto>(beforeStateJson)
                ?? throw new InvalidOperationException("Snapshot data could not be read.");

            var existing = await _context.Users.FirstOrDefaultAsync(u => u.UserId == entityId);

            if (existing != null)
            {
                // Undo an Update or a status change — reapply the snapshot's fields.
                existing.FullNameEnglish = snapshot.FullNameEnglish;
                existing.FullNameNepali = snapshot.FullNameNepali;
                existing.Email = snapshot.Email;
                existing.PhoneNumber = snapshot.PhoneNumber;
                existing.Role = snapshot.Role;
                existing.EmployeeId = snapshot.EmployeeId;
                existing.Department = snapshot.Department;
                existing.Designation = snapshot.Designation;
                existing.WardNumber = snapshot.WardNumber;
                existing.Municipality = snapshot.Municipality;
                existing.District = snapshot.District;
                existing.Province = snapshot.Province;
                existing.AccountStatus = snapshot.AccountStatus;
                existing.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return new RestoreResult { Recreated = false };
            }
            else
            {
                // Undo a Delete — recreate the account. The old password hash is
                // intentionally not restored, so issue a fresh temporary password
                // exactly like creating a brand-new staff account would.
                var temporaryPassword = GenerateTemporaryPassword();

                var restoredUser = new User
                {
                    UserId = entityId,
                    Username = snapshot.Username,
                    Email = snapshot.Email,
                    FullNameEnglish = snapshot.FullNameEnglish,
                    FullNameNepali = snapshot.FullNameNepali,
                    PhoneNumber = snapshot.PhoneNumber,
                    Role = snapshot.Role,
                    EmployeeId = snapshot.EmployeeId,
                    Department = snapshot.Department,
                    Designation = snapshot.Designation,
                    WardNumber = snapshot.WardNumber,
                    Municipality = snapshot.Municipality,
                    District = snapshot.District,
                    Province = snapshot.Province,
                    AccountStatus = snapshot.AccountStatus,
                    IsEmailConfirmed = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                restoredUser.PasswordHash = _passwordHasher.HashPassword(restoredUser, temporaryPassword);

                _context.Users.Add(restoredUser);
                await _context.SaveChangesAsync();

                return new RestoreResult
                {
                    Recreated = true,
                    NewUsername = restoredUser.Username,
                    NewTemporaryPassword = temporaryPassword
                };
            }
        }

        private static string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$";
            var random = new Random();
            return new string(Enumerable.Range(0, 10).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }
    }
}