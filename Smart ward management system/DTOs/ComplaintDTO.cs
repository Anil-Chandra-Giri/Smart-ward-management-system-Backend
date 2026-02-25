using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.DTOs
{
    public class ComplaintDTO
    {
        [Required]
        public string CitizenName { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]{10,15}$")]
        public string ContactNumber { get; set; }

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
        public IFormFile Image { get; set; }
    }
}
