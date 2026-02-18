namespace Smart_ward_management_system.DTOs
{
    public class NoticeDTO
    {
        public string Title { get; set; }
        public string Content { get; set; }
        //public NoticeTypeEnum NoticeType { get; set; }

        public Guid IssuedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsUrgent { get; set; }

        public IFormFile? NoticeFile { get; set; }
    }
}
