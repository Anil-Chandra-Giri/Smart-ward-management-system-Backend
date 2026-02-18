namespace Smart_ward_management_system.DTOs
{
    public class DocumentDTO
    {
        public Guid ReferenceId { get; set; }
        public string ReferenceType { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public string IssuedBy { get; set; }
        public DateTime IssuedDate { get; set; }

        public IFormFile File { get; set; }
    }
}
