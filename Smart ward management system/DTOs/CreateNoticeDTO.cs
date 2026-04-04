// DTOs/CreateNoticeDTO.cs
namespace Smart_ward_management_system.DTOs
{
    public class CreateNoticeDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string Type { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsUrgent { get; set; } = false;
    }

    public class UpdateNoticeDTO
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public Guid? CategoryId { get; set; }
        public string? Type { get; set; }
        public IFormFile? File { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool? IsUrgent { get; set; }
    }

  
}