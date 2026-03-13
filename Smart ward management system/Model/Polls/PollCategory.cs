namespace Smart_ward_management_system.Model.Polls
{
    public class PollCategory
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public ICollection<Poll>? Polls { get; set; }
    }
}
