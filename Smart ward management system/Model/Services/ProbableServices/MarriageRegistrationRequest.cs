namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class MarriageRegistrationRequest : ServiceRequest
    {
        public string GroomFullName { get; set; }
        public string BrideFullName { get; set; }
        public DateTime MarriageDate { get; set; }
        public string MarriageVenue { get; set; }
        public string GroomCitizenshipNo { get; set; }
        public string BrideCitizenshipNo { get; set; }
        public string WitnessName { get; set; }
    }
}
