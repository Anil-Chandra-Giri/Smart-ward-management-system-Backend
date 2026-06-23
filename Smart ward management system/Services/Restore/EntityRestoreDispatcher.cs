using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model;

namespace Smart_ward_management_system.Services.Restore
{
    public interface IEntityRestoreDispatcher
    {
        Task<RestoreResult> RestoreFromLogAsync(int logEntryId);
    }

    public class EntityRestoreDispatcher : IEntityRestoreDispatcher
    {
        private readonly ApplicationDbContext _context;
        private readonly IEnumerable<IEntityRestoreHandler> _handlers;
        private readonly ILoggingService _logger;

        public EntityRestoreDispatcher(
            ApplicationDbContext context,
            IEnumerable<IEntityRestoreHandler> handlers,
            ILoggingService logger)
        {
            _context = context;
            _handlers = handlers;
            _logger = logger;
        }

        public async Task<RestoreResult> RestoreFromLogAsync(int logEntryId)
        {
            var logEntry = await _context.Logs.FirstOrDefaultAsync(l => l.Id == logEntryId);
            if (logEntry == null)
                throw new InvalidOperationException("Log entry not found.");

            if (string.IsNullOrEmpty(logEntry.TargetEntityType) || logEntry.TargetEntityId == null)
                throw new InvalidOperationException("This log entry has no restorable snapshot attached to it.");

            if (string.IsNullOrEmpty(logEntry.BeforeState))
                throw new InvalidOperationException("This log entry has no 'before' state — there's nothing to restore to.");

            var handler = _handlers.FirstOrDefault(h =>
                string.Equals(h.EntityType, logEntry.TargetEntityType, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException($"No restore handler is registered for entity type '{logEntry.TargetEntityType}'.");

            var result = await handler.RestoreAsync(logEntry.TargetEntityId.Value, logEntry.BeforeState);

            // Restoring is itself an auditable action — log it, but don't attach
            // another before/after snapshot to avoid confusing future restores.
            await _logger.LogInfoAsync(
                result.Recreated
                    ? $"Restored a deleted {logEntry.TargetEntityType} record (recreated with a new temporary password)"
                    : $"Restored {logEntry.TargetEntityType} {logEntry.TargetEntityId} to its state from {logEntry.Timestamp:g}",
                LogCategory.Audit,
                new { RestoredFromLogId = logEntryId, TargetEntityType = logEntry.TargetEntityType, TargetEntityId = logEntry.TargetEntityId, Recreated = result.Recreated }
            );

            return result;
        }
    }
}