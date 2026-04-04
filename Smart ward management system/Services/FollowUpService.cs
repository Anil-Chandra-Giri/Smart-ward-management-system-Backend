using Domain.Enumerators;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.FollowUp;
using Smart_ward_management_system.Model.FollowUp;
using Smart_ward_management_system.Model.Services;

namespace Smart_ward_management_system.Services
{
    public interface IFollowUpService
    {
        Task CheckAndProcessOverdueItems();
        Task<List<EscalatedTaskDTO>> GetEscalatedTasksForAdmin(Guid adminId);
        Task<List<AssignmentDTO>> GetAssignmentsForOfficer(Guid officerId);
        Task<bool> SendReminder(ReminderDTO reminder);
        Task<bool> EscalateTask(Guid assignmentId, string reason);
        Task UpdateAssignmentStatus(Guid assignmentId, AssignmentStatus status);
    }

    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly INotificationService _notificationService;

        public FollowUpService(
            ApplicationDbContext db,
            IConfiguration configuration,
            IEmailService emailService,
            INotificationService notificationService)
        {
            _db = db;
            _configuration = configuration;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task CheckAndProcessOverdueItems()
        {
            // Process overdue complaints
            await ProcessOverdueComplaints();

            // Process overdue service requests
            await ProcessOverdueServiceRequests();

            // Save all changes
            await _db.SaveChangesAsync();
        }

        private async Task ProcessOverdueComplaints()
        {
            var overdueComplaints = await _db.Complaints
                .Where(c => c.AssignedOfficerId != null
                    && c.Status != "Resolved"
                    && c.Status != "Closed"
                    && !c.IsEscalated)
                .ToListAsync();

            foreach (var complaint in overdueComplaints)
            {
                var daysOverdue = (DateTime.UtcNow - (complaint.AssignedDate ?? complaint.CreatedAt)).Days;

                // Check if complaint is overdue (7 days threshold)
                if (daysOverdue > 7 && !complaint.IsEscalated)
                {
                    await EscalateComplaint(complaint, daysOverdue);
                }
                // Send reminders for complaints pending > 3 days
                else if (daysOverdue > 3 && daysOverdue <= 7)
                {
                    await SendComplaintReminder(complaint, daysOverdue);
                }
            }
        }

        private async Task ProcessOverdueServiceRequests()
        {
            var overdueServices = await _db.ServiceRequests
                .Where(s => s.AssignedOfficerId != null
                    && s.Status != ApprovalStatusEnum.Approved
                    && s.Status != ApprovalStatusEnum.Rejected
                    && !s.IsEscalated)
                .ToListAsync();

            foreach (var service in overdueServices)
            {
                var daysOverdue = (DateTime.UtcNow - (service.AssignedDate ?? service.CreatedAt)).Days;

                // Check if document request is overdue (3 days threshold)
                if (daysOverdue > 3 && !service.IsEscalated)
                {
                    await EscalateServiceRequest(service, daysOverdue);
                }
                // Send reminders for document requests pending > 2 days
                else if (daysOverdue > 2 && daysOverdue <= 3)
                {
                    await SendServiceReminder(service, daysOverdue);
                }
            }
        }

        private async Task EscalateComplaint(Model.Services.Complaints.Complaint complaint, int daysOverdue)
        {
            // Find next level officer (senior or admin)
            var escalationLevel = daysOverdue > 7 ? EscalationLevel.FirstLevel : EscalationLevel.SecondLevel;
            var escalatedToOfficerId = await GetNextLevelOfficer(complaint.WardNumber, escalationLevel);

            if (escalatedToOfficerId.HasValue)
            {
                complaint.IsEscalated = true;
                complaint.EscalatedDate = DateTime.UtcNow;
                complaint.EscalatedToOfficerId = escalatedToOfficerId;

                // Create escalation record
                var escalation = new EscalationHistory
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = Guid.NewGuid(), // You might want to create assignment record first
                    EscalatedToOfficerId = escalatedToOfficerId.Value,
                    EscalatedByOfficerId = complaint.AssignedOfficerId.Value,
                    EscalationLevel = escalationLevel,
                    EscalationReason = $"Task overdue by {daysOverdue} days",
                    IsActive = true
                };

                _db.EscalationHistories.Add(escalation);

                // Send notification to escalated officer
                await _notificationService.SendNotification(
                    escalatedToOfficerId.Value,
                    "Task Escalated",
                    $"A complaint has been escalated to you. It's overdue by {daysOverdue} days."
                );
            }
        }

        private async Task EscalateServiceRequest(ServiceRequest service, int daysOverdue)
        {
            var escalationLevel = daysOverdue > 3 ? EscalationLevel.FirstLevel : EscalationLevel.SecondLevel;
            var escalatedToOfficerId = await GetNextLevelOfficer(service.RequestedWard, escalationLevel);

            if (escalatedToOfficerId.HasValue)
            {
                service.IsEscalated = true;
                service.EscalatedDate = DateTime.UtcNow;
                service.EscalatedToOfficerId = escalatedToOfficerId;

                var escalation = new EscalationHistory
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = Guid.NewGuid(),
                    EscalatedToOfficerId = escalatedToOfficerId.Value,
                    EscalatedByOfficerId = service.AssignedOfficerId.Value,
                    EscalationLevel = escalationLevel,
                    EscalationReason = $"Document request overdue by {daysOverdue} days",
                    IsActive = true
                };

                _db.EscalationHistories.Add(escalation);

                await _notificationService.SendNotification(
                    escalatedToOfficerId.Value,
                    "Document Request Escalated",
                    $"A document request has been escalated to you. It's overdue by {daysOverdue} days."
                );
            }
        }

        private async Task SendComplaintReminder(Model.Services.Complaints.Complaint complaint, int daysOverdue)
        {
            var reminderType = daysOverdue == 4 ? ReminderType.FirstReminder :
                              daysOverdue == 5 ? ReminderType.SecondReminder :
                              ReminderType.FinalReminder;

            complaint.LastReminderDate = DateTime.UtcNow;
            complaint.ReminderCount++;

            var reminder = new ReminderDTO
            {
                AssignmentId = complaint.ComplaintId,
                OfficerId = complaint.AssignedOfficerId.Value,
                ReferenceType = "Complaint",
                ReferenceNumber = complaint.ComplaintId.ToString().Substring(0, 8),
                DaysOverdue = daysOverdue,
                ReminderType = reminderType
            };

            await SendReminder(reminder);
        }

        private async Task SendServiceReminder(ServiceRequest service, int daysOverdue)
        {
            var reminderType = daysOverdue == 3 ? ReminderType.FirstReminder :
                              daysOverdue == 4 ? ReminderType.SecondReminder :
                              ReminderType.FinalReminder;

            service.LastReminderDate = DateTime.UtcNow;
            service.ReminderCount++;

            var reminder = new ReminderDTO
            {
                AssignmentId = service.ServiceRequestId,
                OfficerId = service.AssignedOfficerId.Value,
                ReferenceType = "Service",
                ReferenceNumber = service.ApplicationNumber,
                DaysOverdue = daysOverdue,
                ReminderType = reminderType
            };

            await SendReminder(reminder);
        }

        public async Task<bool> SendReminder(ReminderDTO reminder)
        {
            try
            {
                // Get officer details
                var officer = await _db.Users.FindAsync(reminder.OfficerId);
                if (officer == null) return false;

                // Create email content
                var subject = $"Reminder: {reminder.ReferenceType} #{reminder.ReferenceNumber} is overdue";
                var body = $@"
                    <h3>Task Reminder</h3>
                    <p>Dear {officer.FullNameEnglish},</p>
                    <p>This is a reminder that the {reminder.ReferenceType.ToLower()} with reference number 
                    <strong>{reminder.ReferenceNumber}</strong> is overdue by <strong>{reminder.DaysOverdue} days</strong>.</p>
                    <p>Please take necessary action immediately.</p>
                    <p>Reminder Type: {reminder.ReminderType}</p>
                    <p>Thank you.</p>
                ";

                // Send email
                await _emailService.SendEmailAsync(officer.Email, subject, body);

                // Send in-app notification
                await _notificationService.SendNotification(
                    reminder.OfficerId,
                    subject,
                    $"Your task #{reminder.ReferenceNumber} is overdue by {reminder.DaysOverdue} days."
                );

                // Log reminder
                var reminderLog = new ReminderLog
                {
                    Id = Guid.NewGuid(),
                    AssignmentId = reminder.AssignmentId,
                    SentToOfficerId = reminder.OfficerId,
                    ReminderType = reminder.ReminderType,
                    SentDate = DateTime.UtcNow
                };

                _db.ReminderLogs.Add(reminderLog);
                await _db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Error sending reminder: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EscalateTask(Guid assignmentId, string reason)
        {
            // Implementation for manual escalation
            return true;
        }

        public async Task<List<EscalatedTaskDTO>> GetEscalatedTasksForAdmin(Guid adminId)
        {
            var escalatedTasks = new List<EscalatedTaskDTO>();

            // Get escalated complaints
            var escalatedComplaints = await _db.Complaints
                .Where(c => c.IsEscalated && c.EscalatedToOfficerId == adminId)
                .Select(c => new EscalatedTaskDTO
                {
                    Id = c.ComplaintId,
                    ReferenceId = c.ComplaintId,
                    ReferenceType = "Complaint",
                    ReferenceNumber = c.ComplaintId.ToString().Substring(0, 8),
                    Title = $"Complaint: {c.Category}",
                    Description = c.ComplaintDetails,
                    OriginalOfficerId = c.AssignedOfficerId ?? Guid.Empty,
                    EscalatedToOfficerName = "Admin", // You'll need to join with Users table
                    EscalatedDate = c.EscalatedDate ?? c.CreatedAt,
                    DaysPending = (DateTime.UtcNow - (c.EscalatedDate ?? c.CreatedAt)).Days,
                    Priority = c.Priority,
                    WardNumber = c.WardNumber,
                    IsHighlighted = true
                })
                .ToListAsync();

            // Get escalated service requests
            var escalatedServices = await _db.ServiceRequests
                .Where(s => s.IsEscalated && s.EscalatedToOfficerId == adminId)
                .Select(s => new EscalatedTaskDTO
                {
                    Id = s.ServiceRequestId,
                    ReferenceId = s.ServiceRequestId,
                    ReferenceType = "Service",
                    ReferenceNumber = s.ApplicationNumber,
                    Title = $"Service: {s.ServiceType}",
                    Description = s.Description ?? s.Purpose,
                    OriginalOfficerId = s.AssignedOfficerId ?? Guid.Empty,
                    EscalatedToOfficerName = "Admin",
                    EscalatedDate = s.EscalatedDate ?? s.CreatedAt,
                    DaysPending = (DateTime.UtcNow - (s.EscalatedDate ?? s.CreatedAt)).Days,
                    Priority = s.PriorityLevel.ToString(),
                    WardNumber = s.RequestedWard,
                    IsHighlighted = true
                })
                .ToListAsync();

            escalatedTasks.AddRange(escalatedComplaints);
            escalatedTasks.AddRange(escalatedServices);

            return escalatedTasks.OrderByDescending(t => t.DaysPending).ToList();
        }

        public async Task<List<AssignmentDTO>> GetAssignmentsForOfficer(Guid officerId)
        {
            var assignments = new List<AssignmentDTO>();

            // Get complaints assigned to officer
            var complaints = await _db.Complaints
                .Where(c => c.AssignedOfficerId == officerId && !c.IsEscalated)
                .Select(c => new AssignmentDTO
                {
                    Id = c.ComplaintId,
                    ReferenceId = c.ComplaintId,
                    ReferenceType = "Complaint",
                    ReferenceNumber = c.ComplaintId.ToString().Substring(0, 8),
                    Title = $"Complaint: {c.Category}",
                    Description = c.ComplaintDetails,
                    AssignedToOfficerId = officerId,
                    AssignedDate = c.AssignedDate ?? c.CreatedAt,
                    LastReminderDate = c.LastReminderDate,
                    ReminderCount = c.ReminderCount,
                    Status = c.Status,
                    IsOverdue = (DateTime.UtcNow - (c.AssignedDate ?? c.CreatedAt)).Days > 7,
                    DaysOverdue = (DateTime.UtcNow - (c.AssignedDate ?? c.CreatedAt)).Days,
                    IsEscalated = c.IsEscalated,
                    Priority = c.Priority,
                    WardNumber = c.WardNumber
                })
                .ToListAsync();

            // Get service requests assigned to officer
            var services = await _db.ServiceRequests
                .Where(s => s.AssignedOfficerId == officerId && !s.IsEscalated)
                .Select(s => new AssignmentDTO
                {
                    Id = s.ServiceRequestId,
                    ReferenceId = s.ServiceRequestId,
                    ReferenceType = "Service",
                    ReferenceNumber = s.ApplicationNumber,
                    Title = $"Service: {s.ServiceType}",
                    Description = s.Description ?? s.Purpose,
                    AssignedToOfficerId = officerId,
                    AssignedDate = s.AssignedDate ?? s.CreatedAt,
                    LastReminderDate = s.LastReminderDate,
                    ReminderCount = s.ReminderCount,
                    Status = s.Status.ToString(),
                    IsOverdue = (DateTime.UtcNow - (s.AssignedDate ?? s.CreatedAt)).Days > 3,
                    DaysOverdue = (DateTime.UtcNow - (s.AssignedDate ?? s.CreatedAt)).Days,
                    IsEscalated = s.IsEscalated,
                    Priority = s.PriorityLevel.ToString(),
                    WardNumber = s.RequestedWard
                })
                .ToListAsync();

            assignments.AddRange(complaints);
            assignments.AddRange(services);

            return assignments.OrderByDescending(a => a.DaysOverdue).ToList();
        }

        private async Task<Guid?> GetNextLevelOfficer(string wardNumber, EscalationLevel level)
        {
            // This should be implemented based on your user hierarchy
            // For now, returning a sample admin ID
            return await _db.Users
                .Where(u => u.Role == "Admin" || u.Role == "SeniorOfficer")
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAssignmentStatus(Guid assignmentId, AssignmentStatus status)
        {
            // Implementation for updating assignment status
        }
    }
}
