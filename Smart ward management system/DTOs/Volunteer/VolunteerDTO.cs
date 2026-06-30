namespace Smart_ward_management_system.DTOs.Volunteer
{
    public class VolunteerDto
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Skills { get; set; }
        public string Availability { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyPhone { get; set; }
        public string ProfilePicture { get; set; }
        public int ActiveAssignments { get; set; }
    }

    public class CreateVolunteerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Skills { get; set; }
        public string Availability { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyPhone { get; set; }
        public IFormFile ProfilePicture { get; set; }
    }

    public class UpdateVolunteerDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Skills { get; set; }
        public string Availability { get; set; }
        public bool IsActive { get; set; }
        public string EmergencyContact { get; set; }
        public string EmergencyPhone { get; set; }
    }

    public class ResourceDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public int MinimumThreshold { get; set; }
        public string Unit { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string StorageLocation { get; set; }
        public string Supplier { get; set; }
        public decimal? UnitPrice { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsLowStock => Quantity <= MinimumThreshold;
    }

    public class CreateResourceDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public int MinimumThreshold { get; set; }
        public string Unit { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string StorageLocation { get; set; }
        public string Supplier { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class UpdateResourceDto
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public int Quantity { get; set; }
        public int MinimumThreshold { get; set; }
        public string Unit { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string StorageLocation { get; set; }
        public string Supplier { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class DisasterEventDto
    {
        public Guid Id { get; set; }
        public string EventName { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public int AffectedPeople { get; set; }
        public string RequiredResources { get; set; }
        public string Coordinator { get; set; }
        public string ContactNumber { get; set; }
        public int AssignedVolunteers { get; set; }
    }

    public class CreateDisasterEventDto
    {
        public string EventName { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime StartDate { get; set; }
        public string Severity { get; set; }
        public int AffectedPeople { get; set; }
        public string RequiredResources { get; set; }
        public string Coordinator { get; set; }
        public string ContactNumber { get; set; }
    }

    public class UpdateDisasterEventDto
    {
        public string EventName { get; set; }
        public string EventType { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public DateTime? EndDate { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public int AffectedPeople { get; set; }
        public string RequiredResources { get; set; }
        public string Coordinator { get; set; }
        public string ContactNumber { get; set; }
    }

    public class VolunteerAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid VolunteerId { get; set; }
        public string VolunteerName { get; set; }
        public Guid DisasterEventId { get; set; }
        public string DisasterEventName { get; set; }
        public string Role { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class CreateVolunteerAssignmentDto
    {
        public Guid VolunteerId { get; set; }
        public Guid DisasterEventId { get; set; }
        public string Role { get; set; }
        public DateTime? StartDate { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateVolunteerAssignmentDto
    {
        public string Role { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }

    public class SelfRegisterVolunteerDto
    {
        // Volunteer fields
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? Address { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Skills { get; set; }
        public string? Availability { get; set; }
        public string? EmergencyContact { get; set; }
        public string? EmergencyPhone { get; set; }

        // Assignment fields
        public Guid DisasterEventId { get; set; }
        public string? Notes { get; set; }
    }

    public class SelfRegisterResponseDto
    {
        public Guid VolunteerId { get; set; }
        public Guid AssignmentId { get; set; }
        public string Message { get; set; }
    }
}
