using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.FollowUp
{
    public class Assignment
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReferenceId { get; set; } // ComplaintId or ServiceRequestId

        [Required]
        public string ReferenceType { get; set; } // "Complaint" or "Service"

        [Required]
        public Guid AssignedToOfficerId { get; set; }

        public Guid? AssignedByAdminId { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastReminderDate { get; set; }

        public int ReminderCount { get; set; } = 0;

        public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;

        // Navigation property
        public ICollection<EscalationHistory> Escalations { get; set; }
    }

    public enum AssignmentStatus
    {
        Active,
        Resolved,
        Escalated,
        Closed
    }
}
