using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.DTOs
{
    public class ComplaintDTO
    {
        [Required]
        public Guid CitizenUserId { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        [MinLength(10)]
        public string ComplaintDetails { get; set; }

        [Required]
        public string Priority { get; set; }

        [Required]
        public string WardNumber { get; set; }

        [Required]
        public string Municipality { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public IFormFile ComplaintImage { get; set; }
    }
}
