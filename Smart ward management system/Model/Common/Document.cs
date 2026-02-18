namespace Smart_ward_management_system.Model.Common
{
    public class Document
    {
        public Guid DocumentId { get; set; }
        public Guid ReferenceId { get; set; }
        public string ReferenceType { get; set; } 
        public string DocumentType { get; set; }  
        public string DocumentNumber { get; set; }
        public string IssuedBy { get; set; }
        public DateTime IssuedDate { get; set; }
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTill { get; set; }
        public string FilePath { get; set; }
        public string? QRHash { get; set; }
        public string? DigitalSignature { get; set; }
        public bool IsVerified { get; set; }
        public Guid? VerifiedByUserId { get; set; }
        public DateTime? VerifiedAt { get; set; }
    }

}
