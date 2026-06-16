using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.FollowUp
{
    public class EscalationHistory
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AssignmentId { get; set; }

        [Required]
        public Guid EscalatedToOfficerId { get; set; }

        public Guid EscalatedByOfficerId { get; set; }

        public DateTime EscalatedDate { get; set; } = DateTime.UtcNow;

        public string EscalationReason { get; set; }

        public EscalationLevel EscalationLevel { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public Assignment Assignment { get; set; }
    }

    public enum EscalationLevel
    {
        FirstLevel,   // Senior Officer
        SecondLevel,  // Admin
        ThirdLevel    // Super Admin
    }
}
