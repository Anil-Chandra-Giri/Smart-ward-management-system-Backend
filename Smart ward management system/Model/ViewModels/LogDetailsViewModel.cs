namespace Smart_ward_management_system.Model.ViewModels
{
    public class LogDetailsViewModel
    {
        public LogEntry Log { get; set; } = new();
        public Dictionary<string, object>? ParsedAdditionalData { get; set; }
        public List<LogEntry> RelatedLogs { get; set; } = new();
    }
}
