namespace Smart_ward_management_system.Model.Logging
{
    public class SystemLog
    {
        public Guid Id { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; }
        public string Exception { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
