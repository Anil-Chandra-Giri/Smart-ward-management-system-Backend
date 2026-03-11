
namespace Smart_ward_management_system.Model.Appointment
{
    public class Queue
    {
        public Guid QueueId { get; set; }
        public int WardNumber { get; set; }
        public string TokenNumber { get; set; }
        public string CitizenName { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; } = "In Queue"; 
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
