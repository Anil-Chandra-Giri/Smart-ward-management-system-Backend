namespace Smart_ward_management_system.Model.Polls
{
    public class PollVote
    {
        public Guid Id { get; set; }

        public Guid PollId { get; set; }

        public Guid OptionId { get; set; }

        public string CitizenId { get; set; } = string.Empty;

        public DateTime VotedOn { get; set; } = DateTime.UtcNow;

        public PollOption? Option { get; set; }

        public Poll? Poll { get; set; }
    }
}
