using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.DTOs.Staff
{
    // Roles an admin is allowed to assign through Manage Staff.
    // Citizens self-register, so "Citizen" is intentionally excluded here.
    public static class AssignableStaffRoles
    {
        public const string Staff = "Staff";
        public const string Admin = "Admin";

        public static readonly string[] All = { Staff, Admin };
    }

    public class StaffListDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullNameEnglish { get; set; } = string.Empty;
        public string FullNameNepali { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string WardNumber { get; set; } = string.Empty;
        public string Municipality { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    public class CreateStaffDto
    {
        [Required, MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string FullNameEnglish { get; set; } = string.Empty;

        public string FullNameNepali { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = AssignableStaffRoles.Staff;

        public string? EmployeeId { get; set; }

        public string? Department { get; set; }

        public string? Designation { get; set; }

        [Required]
        public string WardNumber { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
    }

    public class UpdateStaffDto
    {
        [Required, MaxLength(100)]
        public string FullNameEnglish { get; set; } = string.Empty;

        public string FullNameNepali { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = AssignableStaffRoles.Staff;

        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }

        [Required]
        public string WardNumber { get; set; } = string.Empty;

        public string Municipality { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;

        // "Active" / "Suspended" — matches User.AccountStatus convention
        [Required]
        public string AccountStatus { get; set; } = "Active";
    }

    public class StaffCredentialsDto
    {
        public string Username { get; set; } = string.Empty;
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}