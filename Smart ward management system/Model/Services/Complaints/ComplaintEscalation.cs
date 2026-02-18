using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services.Complaints
{
    public class ComplaintEscalation
    {
        [Key] public Guid EscalationId { get; set; }
        public Guid ComplaintId { get; set; }
        public string EscalatedFrom { get; set; }
        public string EscalatedTo { get; set; }
        public string Reason { get; set; }
        public DateTime EscalatedAt { get; set; }

    }
}
