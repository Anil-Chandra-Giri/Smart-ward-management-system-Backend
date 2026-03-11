namespace Smart_ward_management_system.Model.Polls
{
    public class Poll
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid CategoryId { get; set; }

        public PollCategory? Category { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public bool AllowMultipleVotes { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public string CreatedBy { get; set; } = string.Empty; // Ward Officer Id

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<PollOption>? Options { get; set; }
    }
}
