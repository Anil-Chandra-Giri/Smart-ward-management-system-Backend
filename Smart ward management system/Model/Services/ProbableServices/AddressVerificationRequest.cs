using Microsoft.Extensions.Options;

namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class AddressVerificationRequest : ServiceRequest
    {
        public string HouseNumber { get; set; }
        public string StreetName { get; set; }
        public string WardNumber { get; set; }
        public int YearsOfStay { get; set; }
        public string? MapCoordinate { get; set; }
    }
}
