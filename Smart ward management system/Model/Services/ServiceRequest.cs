using Domain.Enumerators;
using Smart_ward_management_system.Model.Enumerators;

namespace Smart_ward_management_system.Model.Services
{ 
    public class ServiceRequest
    {
        public Guid ServiceRequestId { get; set; }
        public Guid UserId { get; set; }
        public  ServiceEnum ServiceType { get; set; }
        public string ApplicationNumber { get; set; }
        public string Purpose { get; set; }
        public string Description { get; set; }
        public string RequestedWard { get; set; }
        public string RequestedMunicipality { get; set; }
        public PriorityLevelEnum PriorityLevel { get; set; }
        public ApprovalStatusEnum Status { get; set; }
        public Guid? AssignedOfficerId { get; set; }
        public string SubmissionMode { get; set; }        
        public string PaymentStatus { get; set; }         

        public string? Remarks { get; set; }
        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
