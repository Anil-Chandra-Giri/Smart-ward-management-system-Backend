namespace Smart_ward_management_system.Services.Restore
{
    public class RestoreResult
    {
        // True if the entity had been deleted and this restore recreated it
        // (as opposed to just overwriting fields on an entity that still exists).
        public bool Recreated { get; set; }

        // Populated only when Recreated == true — since the old password hash
        // is intentionally not restored, a fresh temporary password is issued,
        // the same way a brand-new staff account would be created.
        public string? NewUsername { get; set; }
        public string? NewTemporaryPassword { get; set; }
    }

    // Implement one of these per entity type that should support restore
    // (Staff, Complaint, ServiceRequest, etc.). Register each in DI as
    // IEntityRestoreHandler — the dispatcher picks the right one by EntityType.
    public interface IEntityRestoreHandler
    {
        // Short key matching LogEntry.TargetEntityType, e.g. "Staff"
        string EntityType { get; }

        // beforeStateJson is LogEntry.BeforeState — the snapshot to restore to.
        Task<RestoreResult> RestoreAsync(Guid entityId, string beforeStateJson);
    }
}