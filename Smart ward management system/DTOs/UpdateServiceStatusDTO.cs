using Domain.Enumerators;

namespace Smart_ward_management_system.DTOs
{
    public class UpdateServiceStatusDTO
    {
        public Guid Id { get; set; }

        public ApprovalStatusEnum Status { get; set; }
    }
}
