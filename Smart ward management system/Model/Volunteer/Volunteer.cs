using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Volunteer
{
    public class Volunteer
    {
            [Key]
            public Guid Id { get; set; } = Guid.NewGuid();

            [Required]
            [StringLength(100)]
            public string FirstName { get; set; }

            [Required]
            [StringLength(100)]
            public string LastName { get; set; }

            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Phone]
            public string PhoneNumber { get; set; }

            public string Address { get; set; }

            public DateTime DateOfBirth { get; set; }

            public string Skills { get; set; } // Comma-separated skills

            public string Availability { get; set; } // Weekdays, Weekends, etc.

            public bool IsActive { get; set; } = true;

            public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

            public string EmergencyContact { get; set; }

            public string EmergencyPhone { get; set; }

            public string ProfilePicture { get; set; }

            // Navigation property
            public virtual ICollection<VolunteerAssignment> Assignments { get; set; }
        }
    
}
