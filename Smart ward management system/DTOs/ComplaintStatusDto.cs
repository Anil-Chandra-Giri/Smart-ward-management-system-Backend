using Domain.Enumerators;

namespace Smart_ward_management_system.DTOs
{
    public class ComplaintStatusDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Remarks { get; set; }
    }
}
