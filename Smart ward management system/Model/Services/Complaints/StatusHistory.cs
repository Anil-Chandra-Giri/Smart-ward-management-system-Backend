using Domain.Enumerators;
using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services.Complaints
{
    public class StatusHistory
    {
        public Guid StatusHistoryId { get; set; }
        public Guid EntityId { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string OldStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public DateTime ChangedAt { get; set; }
        public Guid? ChangedBy { get; set; }
    }
}
