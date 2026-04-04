using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.Common
{
    [Table("UserNotifications")]
    public class UserNotification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; }

        public string? ActionUrl { get; set; }

        public NotificationType Type { get; set; } = NotificationType.Info;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public bool IsDeleted { get; set; } = false;
    }

    public enum NotificationType
    {
        Info = 1,
        Success = 2,
        Warning = 3,
        Error = 4,
        Reminder = 5,
        Escalation = 6
    }
}
