using Microsoft.AspNetCore.Mvc;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.FollowUp;
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

        [HttpGet("officer-assignments/{officerId}")]
        public async Task<IActionResult> GetOfficerAssignments(Guid officerId)
        {
            try
            {
                var assignments = await _followUpService.GetAssignmentsForOfficer(officerId);
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

        [HttpGet("escalated-tasks/{adminId}")]
        public async Task<IActionResult> GetEscalatedTasks(Guid adminId)
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
                if (role == "Officer")
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
                else if (role == "Admin" || role == "SeniorOfficer")
                {
                    var escalatedTasks = await _followUpService.GetEscalatedTasksForAdmin(userId);
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            escalatedTasks = escalatedTasks,
                            totalEscalated = escalatedTasks.Count,
                            criticalTasks = escalatedTasks.Where(t => t.DaysPending > 10).Count(),
                            pendingReview = escalatedTasks.Where(t => t.IsHighlighted).Count()
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
    }
}
