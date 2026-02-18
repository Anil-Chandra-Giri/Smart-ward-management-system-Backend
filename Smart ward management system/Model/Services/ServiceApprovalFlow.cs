using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services
{
    public class ServiceApprovalFlow
    {
        [Key] public Guid ApprovalId { get; set; }
        public Guid ServiceRequestId { get; set; }

    }
}
