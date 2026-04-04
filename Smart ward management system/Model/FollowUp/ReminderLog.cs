using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.FollowUp
{
    public class ReminderLog
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid AssignmentId { get; set; }

        [Required]
        public Guid SentToOfficerId { get; set; }

        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public ReminderType ReminderType { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadDate { get; set; }
    }

    public enum ReminderType
    {
        FirstReminder,
        SecondReminder,
        FinalReminder,
        EscalationNotification
    }
}
