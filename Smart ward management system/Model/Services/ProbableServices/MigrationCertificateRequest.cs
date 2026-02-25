namespace Smart_ward_management_system.Model.Services.ProbableServices
{
    public class MigrationCertificateRequest : ServiceRequest
    {
        public string MigrationType { get; set; } // Incoming or Outgoing
        public string OriginAddress { get; set; }
        public string DestinationAddress { get; set; }
        public int TotalFamilyMembersMoving { get; set; }
        public string ReasonForMigration { get; set; }
    }
}
