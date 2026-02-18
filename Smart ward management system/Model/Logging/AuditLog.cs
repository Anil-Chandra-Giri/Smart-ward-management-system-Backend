using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Logging
{
    public class AuditLog
    {
        [Key] public Guid AuditId { get; set; }
        public Guid UserId { get; set; }
        public string EntityName { get; set; }
        public string EntityId { get; set; }
        public string Action { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}
