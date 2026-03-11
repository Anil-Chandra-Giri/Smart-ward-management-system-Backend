namespace Smart_ward_management_system.DTOs
{
    public class VoteDto
    {
        public Guid PollId { get; set; }

        public Guid OptionId { get; set; }

        public string CitizenId { get; set; } = string.Empty;
    }
}
