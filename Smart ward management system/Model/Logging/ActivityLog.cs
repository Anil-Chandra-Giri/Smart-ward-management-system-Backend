using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Logging
{
    public class ActivityLog
    {
        [Key] public Guid ActivityID { get; set; }
        public Guid UserId { get; set; }
        public string UserFullName { get; set; }
        public string Action { get; set; }
        public string Module { get; set; }
        public string IpAddress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
