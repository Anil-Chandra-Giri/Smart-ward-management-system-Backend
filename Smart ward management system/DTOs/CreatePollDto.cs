namespace Smart_ward_management_system.DTOs
{
    public class CreatePollDto
    {
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid CategoryId { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public List<string> Options { get; set; } = new();
    }
}
