namespace Smart_ward_management_system.DTOs.Staff
{
    // The subset of User fields that Manage Staff actually changes.
    // Deliberately excludes PasswordHash, OTP fields, and citizenship/KYC
    // fields — those aren't part of what staff-management edits, and we
    // don't want sensitive auth data duplicated into the logs table.
    public class StaffSnapshotDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullNameEnglish { get; set; } = string.Empty;
        public string FullNameNepali { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? EmployeeId { get; set; }
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string WardNumber { get; set; } = string.Empty;
        public string Municipality { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string AccountStatus { get; set; } = string.Empty;
    }
}