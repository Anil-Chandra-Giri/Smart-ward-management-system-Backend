namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class DeathCertificateRequest : ServiceRequest
    {
        public string DeceasedFullName { get; set; }
        public DateTime DateOfDeath { get; set; }
        public string PlaceOfDeath { get; set; }
        public string CauseOfDeath { get; set; }
        public string RelationshipToApplicant { get; set; }
        public string CitizenshipNoOfDeceased { get; set; }
    }
}
