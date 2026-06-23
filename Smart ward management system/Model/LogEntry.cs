using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Model.Identity;
using Smart_ward_management_system.Model.Services;
using Smart_ward_management_system.Model.Services.Complaints;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model
{
    public enum LogLevel
    {
        Debug = 0,
        Information = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    public enum LogCategory
    {
        System = 0,
        CitizenServices = 1,
        Grievance = 2,  // Complaint
        TaxCollection = 3,  // Payment
        PropertyRecords = 4,  // PropertyDocumentRequest
        StaffAttendance = 5,
        SchemeImplementation = 6,
        MeetingMinutes = 7,
        Infrastructure = 8,
        ElectionManagement = 9,
        Audit = 10,
        DocumentVerification = 11,  // All certificate requests
        WasteManagement = 12,  // Waste collection
        Notifications = 13,
        Appointments = 14,  // Queue, Token, Appointment
        Polls = 15,
        ServiceRequests = 16,
        UserManagement = 17
    }

    public class LogEntry
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public LogCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Source { get; set; }

        // The ACTOR — who performed the action (populated from JWT claims in LoggingService.LogAsync)
        public Guid? UserId { get; set; }
        public string? UserName { get; set; }

        public string? CitizenId { get; set; }
        public string? WardNumber { get; set; }
        public string? Department { get; set; }
        public string? IpAddress { get; set; }
        public string? RequestPath { get; set; }
        public string? AdditionalData { get; set; }
        public string? ExceptionDetails { get; set; }
        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        // Foreign keys to your existing tables
        public Guid? ComplaintId { get; set; }
        public Guid? ServiceRequestId { get; set; }
        public int? PaymentId { get; set; }
        public Guid? DocumentId { get; set; }
        public Guid? AppointmentId { get; set; }
        public Guid? PollId { get; set; }
        public string? ApplicationId { get; set; }
        public string? GrievanceId { get; set; }

        // ── Restore / rollback support ──────────────────────────────────────
        // The TARGET — which entity was changed (distinct from UserId above,
        // which is the actor; for a staff edit, UserId = the admin who acted,
        // TargetEntityId = the staff account that was changed).
        // TargetEntityType is a short string key like "Staff", "Complaint" —
        // used to look up the right IEntityRestoreHandler.
        public string? TargetEntityType { get; set; }
        public Guid? TargetEntityId { get; set; }

        // JSON snapshots of the entity's restorable fields.
        // BeforeState is null for a Create action (nothing existed before).
        // AfterState is null for a Delete action (nothing exists after).
        public string? BeforeState { get; set; }
        public string? AfterState { get; set; }
    }
}