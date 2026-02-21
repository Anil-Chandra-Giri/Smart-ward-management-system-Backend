namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class RecommendationLetterRequest : ServiceRequest
    {
        public string LetterCategory { get; set; } // e.g., Scholarship, Business, Citizenship
        public string RecipientOrganization { get; set; }
        public string SupportingDocumentsList { get; set; } // JSON or comma-separated
        public bool IsUrgent { get; set; }
    }
}
