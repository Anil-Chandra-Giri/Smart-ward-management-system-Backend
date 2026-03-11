namespace Smart_ward_management_system.Model.Notice
{
    public class Notice
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public NoticeCategory? Category { get; set; } 
        public string? FileUrl { get; set; }
        public NoticeType Type { get; set; }
        public DateTime PublishDate { get; set; } = DateTime.UtcNow;
        public DateTime? ExpiryDate { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsActive { get; set; } = true;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
