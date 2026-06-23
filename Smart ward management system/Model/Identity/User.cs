using Domain.Enumerators;
namespace Smart_ward_management_system.Model.Identity
{
    public class User
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; }
        public string PasswordHash { get; set; }
        public string AccountStatus { get; set; } = "Active";
        public bool IsEmailConfirmed { get; set; } = false;
        public string? OtpCode { get; set; }
        public DateTime? OtpExpiryTime { get; set; }
        public int OtpAttempts { get; set; } = 0;
        public DateTime? LastOtpRequestTime { get; set; }
        public string FullNameNepali { get; set; } = string.Empty;
        public string FullNameEnglish { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string CitizenshipNumber { get; set; } = string.Empty;
        public string CitizenshipIssuedDistrict { get; set; } = string.Empty;
        public DateTime CitizenshipIssuedDate { get; set; }
        public string? NationalIdNumber { get; set; }
        public string PermanentAddress { get; set; } = string.Empty;
        public string TemporaryAddress { get; set; } = string.Empty;
        public string WardNumber { get; set; } = string.Empty;
        public string Municipality { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string ProfilePicturePath { get; set; } = string.Empty;
        public bool IsVerified { get; set; } = false;
        public VerificationStatusEnum VerificationStatus { get; set; } = VerificationStatusEnum.Pending;
        public Guid? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutEnd { get; set; }

        // True until staff changes their temporary password on first login
        public bool IsFirstLogin { get; set; } = true;
    }
}