namespace Smart_ward_management_system.Model.Appointment
{
    public class Token
    {
        public Guid Id { get; set; }
        public string TokenNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
    }
}
