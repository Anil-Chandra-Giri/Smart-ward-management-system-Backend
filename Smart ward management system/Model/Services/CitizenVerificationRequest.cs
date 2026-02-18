using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services
{
    public class CitizenVerificationRequest
    {
        [Key] public Guid VerificationRequestId { get; set; }
        public Guid RequesterUserId { get; set; }
        public string CitizenshipFrontImageUrl { get; set; }
        public string CitizenshipBackImageUrl { get; set; }
        public string LivePhotoUrl { get; set; }
        //public RequestStatusEnum RequestStatus { get; set; }
        public Guid ReviewerUserId { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
