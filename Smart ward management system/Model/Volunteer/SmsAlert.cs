using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.Volunteer
{
    public class SmsAlert
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Message { get; set; }

        public string RecipientGroup { get; set; } // All, Volunteers, Specific

        public DateTime SentDate { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } // Pending, Sent, Failed

        public int RecipientCount { get; set; }

        public int SuccessCount { get; set; }

        public int FailedCount { get; set; }

        [ForeignKey("SentBy")]
        public Guid? SentByUserId { get; set; }

        [ForeignKey("DisasterEvent")]
        public Guid? DisasterEventId { get; set; }

        // Navigation properties
        public virtual DisasterEvent DisasterEvent { get; set; }
    }
}
