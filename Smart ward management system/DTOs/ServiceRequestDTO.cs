namespace Smart_ward_management_system.DTOs
{
    public class ServiceRequestDTO
    {
        public Guid UserId { get; set; }
        public string ServiceType { get; set; }
        public string Purpose { get; set; }
        public string Description { get; set; }
        public string RequestedWard { get; set; }
        public string RequestedMunicipality { get; set; }
        //public PriorityLevelEnum PriorityLevel { get; set; }
    }
}
