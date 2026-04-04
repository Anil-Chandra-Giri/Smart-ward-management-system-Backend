using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Volunteer
{
    public class DisasterEvent
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(200)]
        public string EventName { get; set; }

        [Required]
        public string EventType { get; set; } // Flood, Earthquake, Fire, etc.

        public string Description { get; set; }

        public string Location { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Severity { get; set; } // Low, Medium, High, Critical

        public string Status { get; set; } // Active, Inactive, Completed

        public int AffectedPeople { get; set; }

        public string RequiredResources { get; set; }

        public string Coordinator { get; set; }

        public string ContactNumber { get; set; }

        // Navigation properties
        public virtual ICollection<VolunteerAssignment> VolunteerAssignments { get; set; }
    }
}
