using Domain.Enumerators;
using Smart_ward_management_system.Model.Enumerators;
using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services.Complaints
{
    public class Complaint
    {
        [Key]
        public Guid ComplaintId { get; set; }

        [Required]
        [StringLength(100)]
        public string CitizenName { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{10,15}$", ErrorMessage = "Invalid contact number format.")]
        public string ContactNumber { get; set; }

        [Required]
        public string Category { get; set; } = "Waste Management";

        [Required]
        [MinLength(10)]
        public string ComplaintDetails { get; set; }

        [Required]
        public string Priority { get; set; } = "Normal";

        [Required]
        public string WardNumber { get; set; }

        [Required]
        public string Municipality { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";

    }
}
