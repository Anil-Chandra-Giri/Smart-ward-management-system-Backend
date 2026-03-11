namespace Smart_ward_management_system.DTOs
{
    public class AppointmentDto
    {
        public string CitizenName { get; set; }
        public string ContactNumber { get; set; }
        public string ServiceType { get; set; }
        public int WardNumber { get; set; }
        public DateTime AppointmentTime { get; set; }
    }
}
