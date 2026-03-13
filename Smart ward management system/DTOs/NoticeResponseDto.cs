namespace Smart_ward_management_system.DTOs
{
    public class NoticeResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? FileUrl { get; set; }
        public bool IsUrgent { get; set; }
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
