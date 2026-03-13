namespace Smart_ward_management_system.Model.Appointment
{
    public class Token
    {
        public Guid TokenId { get; set; }  
        public Guid AppointmentId { get; set; }
        public int TokenSequence { get; set; }
        public string? TokenNumber { get; set; }
        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
