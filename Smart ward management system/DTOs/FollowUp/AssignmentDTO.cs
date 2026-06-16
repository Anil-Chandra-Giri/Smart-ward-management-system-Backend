using Smart_ward_management_system.Model.FollowUp;

namespace Smart_ward_management_system.DTOs.FollowUp
{
    public class AssignmentDTO
    {
        public Guid Id { get; set; }
        public Guid ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public string ReferenceNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Guid AssignedToOfficerId { get; set; }
        public string AssignedToOfficerName { get; set; }
        public DateTime AssignedDate { get; set; }
        public DateTime? LastReminderDate { get; set; }
        public int ReminderCount { get; set; }
        public string Status { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
        public bool IsEscalated { get; set; }
        public EscalationLevel? CurrentEscalationLevel { get; set; }
        public string Priority { get; set; }
        public string WardNumber { get; set; }
    }

    public class EscalatedTaskDTO
    {
        public Guid Id { get; set; }
        public Guid ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public string ReferenceNumber { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string OriginalOfficerName { get; set; }
        public Guid OriginalOfficerId { get; set; }
        public string EscalatedToOfficerName { get; set; }
        public DateTime EscalatedDate { get; set; }
        public EscalationLevel EscalationLevel { get; set; }
        public int DaysPending { get; set; }
        public string Priority { get; set; }
        public string WardNumber { get; set; }
        public bool IsHighlighted { get; set; } = true;
    }

    public class ReminderDTO
    {
        public Guid AssignmentId { get; set; }
        public Guid OfficerId { get; set; }
        public string OfficerEmail { get; set; }
        public string OfficerName { get; set; }
        public string ReferenceType { get; set; }
        public string ReferenceNumber { get; set; }
        public int DaysOverdue { get; set; }
        public ReminderType ReminderType { get; set; }
    }
}
