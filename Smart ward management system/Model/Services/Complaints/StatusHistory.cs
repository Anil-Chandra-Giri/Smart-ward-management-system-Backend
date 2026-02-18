using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services.Complaints
{
    public class StatusHistory
    {
        [Key] public Guid HistoryId { get; set; }
        public Guid ReferenceId { get; set; }
        //public ReferenceTypeEnum ReferenceType { get; set; }
        public Guid OldStatusId { get; set; }
        public Guid NewStatusId { get; set; }
        public Guid ChangedBy { get; set; }
        public string ChangeReason { get; set; }
        public DateTime ChangedAt { get; set; }

    }
}
