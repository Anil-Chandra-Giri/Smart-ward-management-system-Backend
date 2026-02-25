namespace Smart_ward_management_system.DTOs
{
    public class ServiceRequestDTO
    {
        // Base Service Fields
        public Guid UserId { get; set; }
        public int ServiceType { get; set; }
        public string Purpose { get; set; }
        public string Description { get; set; }
        public string RequestedWard { get; set; }
        public string RequestedMunicipality { get; set; }
        public int PriorityLevel { get; set; }
        public string? Remarks { get; set; }
        public string SubmissionMode { get; set; } = "Online";

        // Birth Certificate Fields
        public string? ChildFullName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PlaceOfBirth { get; set; }
        public string? FatherFullName { get; set; }
        public string? MotherFullName { get; set; }
        public string? GrandfatherFullName { get; set; }
        public string? PermanentAddress { get; set; }

        // Death Certificate Fields
        public string? DeceasedFullName { get; set; }
        public DateTime? DateOfDeath { get; set; }
        public string? PlaceOfDeath { get; set; }
        public string? CauseOfDeath { get; set; }
        public string? RelationshipToApplicant { get; set; }
        public string? CitizenshipNoOfDeceased { get; set; }

        // Recommendation Letter Fields
        public string? LetterCategory { get; set; }
        public string? RecipientOrganization { get; set; }

        // Property Document Fields
        public string? PlotNumber { get; set; }
        public string? SheetNumber { get; set; }
        public double? TotalArea { get; set; }
        public string? PropertyType { get; set; }
        public string? CurrentOwnerName { get; set; }
        public string? LandRevenueReceiptNumber { get; set; }

        // Marriage Registration Fields
        public string? GroomFullName { get; set; }
        public string? BrideFullName { get; set; }
        public DateTime? MarriageDate { get; set; }
        public string? MarriageVenue { get; set; }
        public string? GroomCitizenshipNo { get; set; }
        public string? BrideCitizenshipNo { get; set; }
        public string? WitnessName { get; set; }

        // Migration Certificate Fields
        public string? MigrationType { get; set; }
        public string? OriginAddress { get; set; }
        public string? DestinationAddress { get; set; }
        public int? TotalFamilyMembersMoving { get; set; }
        public string? ReasonForMigration { get; set; }

        // Address Verification Fields
        public string? HouseNumber { get; set; }
        public string? StreetName { get; set; }
        public int? YearsOfStay { get; set; }
    }
}
