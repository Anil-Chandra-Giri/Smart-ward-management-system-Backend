namespace Smart_ward_management_system.Model.Polls
{
    public class PollOption
    {
        public Guid Id { get; set; }

        public Guid PollId { get; set; }

        public Poll? Poll { get; set; }

        public string OptionText { get; set; } = string.Empty;

        public ICollection<PollVote>? Votes { get; set; }
    }
}
