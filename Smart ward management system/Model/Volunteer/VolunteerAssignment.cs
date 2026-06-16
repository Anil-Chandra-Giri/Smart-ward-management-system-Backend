using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.Volunteer
{
    public class VolunteerAssignment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Volunteer")]
        public Guid VolunteerId { get; set; }

        [ForeignKey("DisasterEvent")]
        public Guid DisasterEventId { get; set; }

        public string Role { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string Status { get; set; } // Assigned, InProgress, Completed, Cancelled

        public string Notes { get; set; }

        // Navigation properties
        public virtual Volunteer Volunteer { get; set; }
        public virtual DisasterEvent DisasterEvent { get; set; }
    }
}
