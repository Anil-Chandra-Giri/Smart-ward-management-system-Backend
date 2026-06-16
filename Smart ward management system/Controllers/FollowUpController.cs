using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.FollowUp;
using Smart_ward_management_system.Model.Common;
using Smart_ward_management_system.Services;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FollowUpController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;
        private readonly ApplicationDbContext _db;

        public FollowUpController(IFollowUpService followUpService, ApplicationDbContext db)
        {
            _followUpService = followUpService;
            _db = db;
        }

        // ============ STAFF ENDPOINTS ============

        [HttpGet("staff-assignments/{staffId}")]
        public async Task<IActionResult> GetStaffAssignments(Guid staffId)
        {
            try
            {
                var assignments = await _followUpService.GetAssignmentsForOfficer(staffId);
                return Ok(new
                {
                    success = true,
                    data = assignments,
                    overdueCount = assignments.Count(a => a.IsOverdue),
                    escalatedCount = assignments.Count(a => a.IsEscalated)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ ADMIN ENDPOINTS ============

        [HttpGet("admin/escalated-tasks/{adminId}")]
        public async Task<IActionResult> GetEscalatedTasksForAdmin(Guid adminId)
        {
            try
            {
                var tasks = await _followUpService.GetEscalatedTasksForAdmin(adminId);
                return Ok(new
                {
                    success = true,
                    data = tasks,
                    totalCount = tasks.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("admin/all-overdue-tasks")]
        public async Task<IActionResult> GetAllOverdueTasks()
        {
            try
            {
                // Get all overdue complaints
                var overdueComplaints = await _db.Complaints
                    .Where(c => c.Status != "Resolved" && c.Status != "Closed")
                    .Select(c => new
                    {
                        id = c.ComplaintId,
                        referenceType = "Complaint",
                        referenceNumber = c.ComplaintId.ToString().Substring(0, 8),
                        title = $"Complaint: {c.Category}",
                        description = c.ComplaintDetails,
                        assignedToOfficerId = c.AssignedOfficerId,
                        assignedDate = c.AssignedDate ?? c.CreatedAt,
                        daysOverdue = (DateTime.UtcNow - (c.AssignedDate ?? c.CreatedAt)).Days,
                        priority = c.Priority,
                        wardNumber = c.WardNumber,
                        status = c.Status,
                        isEscalated = c.IsEscalated
                    })
                    .ToListAsync();

                // Get all overdue service requests
                var overdueServices = await _db.ServiceRequests
                    .Where(s => s.Status != Domain.Enumerators.ApprovalStatusEnum.Approved
                                && s.Status != Domain.Enumerators.ApprovalStatusEnum.Rejected)
                    .Select(s => new
                    {
                        id = s.ServiceRequestId,
                        referenceType = "Service",
                        referenceNumber = s.ApplicationNumber,
                        title = $"Service: {s.ServiceType}",
                        description = s.Description ?? s.Purpose,
                        assignedToOfficerId = s.AssignedOfficerId,
                        assignedDate = s.AssignedDate ?? s.CreatedAt,
                        daysOverdue = (DateTime.UtcNow - (s.AssignedDate ?? s.CreatedAt)).Days,
                        priority = s.PriorityLevel.ToString(),
                        wardNumber = s.RequestedWard,
                        status = s.Status.ToString(),
                        isEscalated = s.IsEscalated
                    })
                    .ToListAsync();

                var allOverdueTasks = overdueComplaints
                    .Concat(overdueServices)
                    .Where(t => t.daysOverdue > 0)
                    .OrderByDescending(t => t.daysOverdue)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = allOverdueTasks,
                    totalCount = allOverdueTasks.Count,
                    criticalCount = allOverdueTasks.Count(t => t.daysOverdue > 10)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ CITIZEN ENDPOINTS ============

        [HttpGet("citizen/complaints/{citizenId}")]
        public async Task<IActionResult> GetCitizenComplaints(Guid citizenId)
        {
            try
            {
                var complaints = await _db.Complaints
                    .Where(c => c.CitizenUserId == citizenId)
                    .Select(c => new
                    {
                        id = c.ComplaintId,
                        category = c.Category,
                        details = c.ComplaintDetails,
                        status = c.Status,
                        priority = c.Priority,
                        createdAt = c.CreatedAt,
                        assignedOfficerId = c.AssignedOfficerId,
                        isEscalated = c.IsEscalated,
                        reminderCount = c.ReminderCount,
                        imageUrl = c.ImageUrl
                    })
                    .OrderByDescending(c => c.createdAt)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = complaints,
                    totalCount = complaints.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("citizen/service-requests/{citizenId}")]
        public async Task<IActionResult> GetCitizenServiceRequests(Guid citizenId)
        {
            try
            {
                var services = await _db.ServiceRequests
                    .Where(s => s.UserId == citizenId)
                    .Select(s => new
                    {
                        id = s.ServiceRequestId,
                        applicationNumber = s.ApplicationNumber,
                        serviceType = s.ServiceType.ToString(),
                        purpose = s.Purpose,
                        description = s.Description,
                        status = s.Status.ToString(),
                        priority = s.PriorityLevel.ToString(),
                        createdAt = s.CreatedAt,
                        assignedOfficerId = s.AssignedOfficerId,
                        isEscalated = s.IsEscalated
                    })
                    .OrderByDescending(s => s.createdAt)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = services,
                    totalCount = services.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("citizen/track/{complaintId}")]
        public async Task<IActionResult> TrackComplaintStatus(Guid complaintId)
        {
            try
            {
                var complaint = await _db.Complaints
                    .Where(c => c.ComplaintId == complaintId)
                    .Select(c => new
                    {
                        id = c.ComplaintId,
                        status = c.Status,
                        assignedOfficerId = c.AssignedOfficerId,
                        assignedDate = c.AssignedDate,
                        reminderCount = c.ReminderCount,
                        isEscalated = c.IsEscalated,
                        escalatedDate = c.EscalatedDate,
                        createdAt = c.CreatedAt,
                        estimatedResolution = c.AssignedDate.HasValue
                            ? c.AssignedDate.Value.AddDays(7)
                            : c.CreatedAt.AddDays(7)
                    })
                    .FirstOrDefaultAsync();

                if (complaint == null)
                {
                    return NotFound(new { success = false, message = "Complaint not found" });
                }

                // Get escalation history if any
                List<object> escalationHistory = new List<object>();
                if (complaint.isEscalated)
                {
                    escalationHistory = await _db.EscalationHistories
                        .Where(e => e.Assignment.ReferenceId == complaintId)
                        .Select(e => new
                        {
                            escalatedTo = e.EscalatedToOfficerId,
                            escalatedDate = e.EscalatedDate,
                            reason = e.EscalationReason,
                            level = e.EscalationLevel
                        })
                        .ToListAsync<object>();
                }

                return Ok(new
                {
                    success = true,
                    data = complaint,
                    escalationHistory = escalationHistory
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ COMMON ENDPOINTS ============

        [HttpPost("send-reminder")]
        public async Task<IActionResult> SendManualReminder([FromBody] ReminderDTO reminder)
        {
            try
            {
                var result = await _followUpService.SendReminder(reminder);
                if (result)
                {
                    return Ok(new { success = true, message = "Reminder sent successfully" });
                }
                return BadRequest(new { success = false, message = "Failed to send reminder" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost("escalate/{assignmentId}")]
        public async Task<IActionResult> EscalateTask(Guid assignmentId, [FromBody] string reason)
        {
            try
            {
                var result = await _followUpService.EscalateTask(assignmentId, reason);
                if (result)
                {
                    return Ok(new { success = true, message = "Task escalated successfully" });
                }
                return BadRequest(new { success = false, message = "Failed to escalate task" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("dashboard-stats/{userId}")]
        public async Task<IActionResult> GetDashboardStats(Guid userId, string role)
        {
            try
            {
                // STAFF DASHBOARD
                if (role == "Staff")
                {
                    var assignments = await _followUpService.GetAssignmentsForOfficer(userId);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            totalTasks = assignments.Count,
                            overdueTasks = assignments.Count(a => a.IsOverdue),
                            escalatedToMe = assignments.Count(a => a.IsEscalated),
                            pendingTasks = assignments.Count(a => !a.IsOverdue && !a.IsEscalated),
                            recentReminders = assignments.Where(a => a.LastReminderDate != null)
                                                        .OrderByDescending(a => a.LastReminderDate)
                                                        .Take(5)
                                                        .ToList()
                        }
                    });
                }
                // ADMIN DASHBOARD
                else if (role == "Admin")
                {
                    var escalatedTasks = await _followUpService.GetEscalatedTasksForAdmin(userId);

                    // Get overall stats
                    var totalComplaints = await _db.Complaints.CountAsync();
                    var pendingComplaints = await _db.Complaints.CountAsync(c => c.Status == "Pending");
                    var resolvedComplaints = await _db.Complaints.CountAsync(c => c.Status == "Resolved");

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            escalatedTasks = escalatedTasks,
                            totalEscalated = escalatedTasks.Count,
                            criticalTasks = escalatedTasks.Where(t => t.DaysPending > 10).Count(),
                            pendingReview = escalatedTasks.Where(t => t.IsHighlighted).Count(),
                            totalComplaints = totalComplaints,
                            pendingComplaints = pendingComplaints,
                            resolvedComplaints = resolvedComplaints
                        }
                    });
                }
                // CITIZEN DASHBOARD
                else if (role == "Citizen")
                {
                    var myComplaints = await _db.Complaints
                        .Where(c => c.CitizenUserId == userId)
                        .CountAsync();

                    var pendingComplaints = await _db.Complaints
                        .Where(c => c.CitizenUserId == userId && c.Status == "Pending")
                        .CountAsync();

                    var resolvedComplaints = await _db.Complaints
                        .Where(c => c.CitizenUserId == userId && c.Status == "Resolved")
                        .CountAsync();

                    var escalatedComplaints = await _db.Complaints
                        .Where(c => c.CitizenUserId == userId && c.IsEscalated)
                        .CountAsync();

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            totalComplaints = myComplaints,
                            pendingComplaints = pendingComplaints,
                            resolvedComplaints = resolvedComplaints,
                            escalatedComplaints = escalatedComplaints
                        }
                    });
                }

                return BadRequest(new { success = false, message = "Invalid role" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ NOTIFICATION ENDPOINTS (All Roles) ============

        [HttpGet("Notification/user/{userId}")]
        public async Task<IActionResult> GetUserNotifications(Guid userId, [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var query = _db.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsDeleted);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var notifications = await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(100)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Notification/mark-read/{notificationId}")]
        public async Task<IActionResult> MarkNotificationAsRead(Guid notificationId)
        {
            try
            {
                var notification = await _db.UserNotifications.FindAsync(notificationId);
                if (notification == null)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "Notification marked as read" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPut("Notification/mark-all-read/{userId}")]
        public async Task<IActionResult> MarkAllNotificationsAsRead(Guid userId)
        {
            try
            {
                var unreadNotifications = await _db.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"{unreadNotifications.Count} notifications marked as read",
                    count = unreadNotifications.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("Notification/unread-count/{userId}")]
        public async Task<IActionResult> GetUnreadNotificationCount(Guid userId)
        {
            try
            {
                var count = await _db.UserNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);

                return Ok(new
                {
                    success = true,
                    data = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("Notification/{notificationId}")]
        public async Task<IActionResult> DeleteNotification(Guid notificationId)
        {
            try
            {
                var notification = await _db.UserNotifications.FindAsync(notificationId);
                if (notification == null)
                {
                    return NotFound(new { success = false, message = "Notification not found" });
                }

                notification.IsDeleted = true;
                await _db.SaveChangesAsync();

                return Ok(new { success = true, message = "Notification deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ============ MANUAL TRIGGER FOR TESTING ============

        [HttpPost("trigger-escalation-check")]
        public async Task<IActionResult> TriggerEscalationCheck()
        {
            try
            {
                await _followUpService.CheckAndProcessOverdueItems();
                return Ok(new { success = true, message = "Escalation check triggered successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}