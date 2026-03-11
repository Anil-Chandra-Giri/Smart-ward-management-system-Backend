namespace Smart_ward_management_system.Model.Appointment
{
    public class Appointment
    {
        public Guid AppointmentId { get; set; }     
        public string CitizenName { get; set; }
        public string ContactNumber { get; set; }
        public string ServiceType { get; set; }
        public int WardNumber { get; set; }
        public DateTime AppointmentTime { get; set; }
        public string Status { get; set; } = "Pending"; 
        public string? TokenNumber { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
