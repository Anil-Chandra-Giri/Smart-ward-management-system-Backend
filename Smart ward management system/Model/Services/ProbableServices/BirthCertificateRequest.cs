namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class BirthCertificateRequest : ServiceRequest
    {
        public string ChildFullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string PlaceOfBirth { get; set; } // Hospital/Home
        public string FatherFullName { get; set; }
        public string MotherFullName { get; set; }
        public string GrandfatherFullName { get; set; }
        public string PermanentAddress { get; set; }
    }
}
