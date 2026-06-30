using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
using Smart_ward_management_system.Filters;
using Smart_ward_management_system.Model;
using Smart_ward_management_system.Model.Appointment;
using Smart_ward_management_system.Services;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<RealTimeHub> _hubContext;
        private readonly ILoggingService _logger;

        public AppointmentController(ApplicationDbContext context, IHubContext<RealTimeHub> hubContext, ILoggingService logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        // GET: api/Appointment/queue/{wardNumber}
        [HttpGet("queue/{wardNumber}")]
        public async Task<IActionResult> GetQueueByWard(int wardNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching queue for ward: {wardNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });

                var queue = await _context.Queues
                    .Where(q => q.WardNumber == wardNumber && q.Status == "In Queue")
                    .OrderBy(q => q.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {queue.Count} queue entries for ward {wardNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber, Count = queue.Count });

                return Ok(queue);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching queue for ward {wardNumber}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });
                return StatusCode(500, new { message = "An error occurred while fetching the queue." });
            }
        }

        // GET: api/Appointment/GetAllAppointments
        [HttpGet("GetAllAppointments")]
        public async Task<IActionResult> GetAllAppointments()
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching all appointments",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId });

                var appointments = await _context.Appointments
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {appointments.Count} total appointments",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, Count = appointments.Count });

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching all appointments",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId });
                return StatusCode(500, new { message = "An error occurred while fetching appointments." });
            }
        }

        // GET: api/Appointment/MyAppointments?userId={userId}
        [HttpGet("MyAppointments")]
        public async Task<IActionResult> GetMyAppointments([FromQuery] Guid userId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                if (userId == Guid.Empty)
                {
                    await _logger.LogWarningAsync($"Invalid or missing UserId",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId });
                    return BadRequest(new { message = "Valid UserId is required." });
                }

                await _logger.LogInfoAsync($"Fetching appointments for user: {userId}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, UserId = userId });

                var myAppointments = await _context.Appointments
                    .Where(a => a.UserId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {myAppointments.Count} appointments for user {userId}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, UserId = userId, Count = myAppointments.Count });

                return Ok(myAppointments);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching appointments for user: {userId}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, UserId = userId });
                return StatusCode(500, new { message = "An error occurred while fetching appointments." });
            }
        }

        // GET: api/Appointment/appointment/{id}
        [HttpGet("appointment/{id}")]
        public async Task<IActionResult> GetAppointmentById(Guid id)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching appointment by ID: {id}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = id });

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == id);

                if (appointment == null)
                {
                    await _logger.LogWarningAsync($"Appointment not found with ID: {id}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, AppointmentId = id });
                    return NotFound(new { message = "Appointment not found" });
                }

                await _logger.LogInfoAsync($"Retrieved appointment {id} with status: {appointment.Status}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = id, Status = appointment.Status });

                return Ok(appointment);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching appointment by ID: {id}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = id });
                return StatusCode(500, new { message = "An error occurred while fetching the appointment." });
            }
        }

        // GET: api/Appointment/token/{tokenNumber}
        [HttpGet("token/{tokenNumber}")]
        public async Task<IActionResult> GetTokenStatus(string tokenNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching token status for: {tokenNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, TokenNumber = tokenNumber });

                var queue = await _context.Queues
                    .FirstOrDefaultAsync(q => q.TokenNumber == tokenNumber);

                if (queue == null)
                {
                    await _logger.LogWarningAsync($"Token not found: {tokenNumber}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, TokenNumber = tokenNumber });
                    return NotFound(new { message = "Token not found" });
                }

                return Ok(new { queue.TokenNumber, queue.Status, queue.CitizenName, queue.ServiceType });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching token status for: {tokenNumber}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, TokenNumber = tokenNumber });
                return StatusCode(500, new { message = "An error occurred while fetching token status." });
            }
        }

        // POST: api/Appointment/book
        [HttpPost("book")]
        [RequireVerifiedCitizen]
        public async Task<IActionResult> BookAppointment([FromBody] AppointmentDto appointment)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                if (appointment == null)
                {
                    await _logger.LogWarningAsync($"Book appointment called with null data",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId });
                    return BadRequest("Invalid appointment data.");
                }

                if (appointment.UserId == Guid.Empty)
                {
                    await _logger.LogWarningAsync($"Invalid UserId provided in appointment request",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId });
                    return BadRequest(new { message = "Valid UserId is required." });
                }

                await _logger.LogInfoAsync($"Booking appointment for citizen: {appointment.CitizenName}, Ward: {appointment.WardNumber}, Service: {appointment.ServiceType}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        CitizenName = appointment.CitizenName,
                        WardNumber = appointment.WardNumber,
                        ServiceType = appointment.ServiceType,
                        ContactNumber = appointment.ContactNumber,
                        UserId = appointment.UserId
                    });

                var newAppointment = new Appointment
                {
                    AppointmentId = Guid.NewGuid(),
                    UserId = appointment.UserId,
                    CitizenName = appointment.CitizenName,
                    ContactNumber = appointment.ContactNumber,
                    ServiceType = appointment.ServiceType,
                    WardNumber = appointment.WardNumber,
                    AppointmentTime = appointment.AppointmentTime,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Appointments.Add(newAppointment);
                await _context.SaveChangesAsync();

                await _logger.LogInfoAsync($"Appointment created with ID: {newAppointment.AppointmentId}, UserId: {newAppointment.UserId}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        AppointmentId = newAppointment.AppointmentId,
                        UserId = newAppointment.UserId
                    });

                // Create token
                var token = new Token
                {
                    TokenId = Guid.NewGuid(),
                    AppointmentId = newAppointment.AppointmentId,
                    Status = "Active",
                    IssuedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Tokens.Add(token);
                await _context.SaveChangesAsync();

                // Generate token number
                string tokenNumber = $"TKN-{DateTime.Now:yyyyMMdd}-{token.TokenSequence:D4}";
                token.TokenNumber = tokenNumber;
                newAppointment.TokenNumber = tokenNumber;
                await _context.SaveChangesAsync();

                // Create queue entry
                var queue = new Queue
                {
                    QueueId = Guid.NewGuid(),
                    WardNumber = newAppointment.WardNumber,
                    TokenNumber = tokenNumber,
                    CitizenName = newAppointment.CitizenName,
                    ServiceType = newAppointment.ServiceType,
                    Status = "In Queue",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Queues.Add(queue);
                await _context.SaveChangesAsync();

                await _logger.LogAppointmentAsync(newAppointment.AppointmentId, appointment.CitizenName, "booked");

                await _logger.LogInfoAsync($"Queue entry created - Queue ID: {queue.QueueId}, Token: {tokenNumber}, Status: In Queue",
                    LogCategory.Appointments,
                    new { QueueId = queue.QueueId, TokenNumber = tokenNumber });

                await _logger.LogCitizenActionAsync(
                    appointment.ContactNumber,
                    $"Booked appointment for {appointment.ServiceType} - Token: {tokenNumber}",
                    "Appointment Booking"
                );

                await _logger.LogInfoAsync($"Appointment booked successfully. Token: {tokenNumber}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        AppointmentId = newAppointment.AppointmentId,
                        TokenNumber = tokenNumber,
                        QueueId = queue.QueueId,
                        UserId = newAppointment.UserId
                    });

                await _hubContext.Clients.All.SendAsync("ReceiveNewToken", new
                {
                    TokenNumber = tokenNumber,
                    CitizenName = appointment.CitizenName,
                    ServiceType = appointment.ServiceType,
                    WardNumber = appointment.WardNumber
                });

                return Ok(new
                {
                    newAppointment.AppointmentId,
                    newAppointment.UserId,
                    TokenNumber = tokenNumber,
                    QueuePosition = queue.QueueId,
                    Message = "Appointment booked successfully!"
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error booking appointment for citizen: {appointment?.CitizenName}",
                    ex,
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        CitizenName = appointment?.CitizenName,
                        WardNumber = appointment?.WardNumber,
                        UserId = appointment?.UserId,
                        ErrorMessage = ex.Message
                    });
                return StatusCode(500, new { message = "An error occurred while booking the appointment." });
            }
        }

        // ============ UPDATE APPOINTMENT ENDPOINT ============
        // PUT: api/Appointment/update/{appointmentId}
        [HttpPut("update/{appointmentId}")]
        public async Task<IActionResult> UpdateAppointment(Guid appointmentId, [FromBody] UpdateAppointmentDto updatedAppointment)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Updating appointment: {appointmentId}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = appointmentId });

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    await _logger.LogWarningAsync($"Appointment not found for update: {appointmentId}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, AppointmentId = appointmentId });
                    return NotFound(new { message = "Appointment not found" });
                }

                // Check if appointment can be updated
                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    await _logger.LogWarningAsync($"Attempted to update {appointment.Status} appointment: {appointmentId}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, AppointmentId = appointmentId, Status = appointment.Status });
                    return BadRequest(new { message = $"Cannot update {appointment.Status} appointment" });
                }

                // Update fields
                if (!string.IsNullOrEmpty(updatedAppointment.CitizenName))
                    appointment.CitizenName = updatedAppointment.CitizenName;

                if (!string.IsNullOrEmpty(updatedAppointment.ContactNumber))
                    appointment.ContactNumber = updatedAppointment.ContactNumber;

                if (!string.IsNullOrEmpty(updatedAppointment.ServiceType))
                    appointment.ServiceType = updatedAppointment.ServiceType;

                if (updatedAppointment.WardNumber > 0)
                    appointment.WardNumber = updatedAppointment.WardNumber;

                if (updatedAppointment.AppointmentTime != DateTime.MinValue)
                    appointment.AppointmentTime = updatedAppointment.AppointmentTime;

                // Only update status if provided and valid
                if (!string.IsNullOrEmpty(updatedAppointment.Status))
                {
                    var validStatuses = new[] { "Pending", "Completed", "Cancelled" };
                    if (validStatuses.Contains(updatedAppointment.Status))
                    {
                        // If status is being changed to Cancelled, also update the queue
                        if (updatedAppointment.Status == "Cancelled" && appointment.Status != "Cancelled")
                        {
                            var queue = await _context.Queues
                                .FirstOrDefaultAsync(q => q.TokenNumber == appointment.TokenNumber);
                            if (queue != null)
                            {
                                queue.Status = "Cancelled";
                                queue.UpdatedAt = DateTime.Now;
                            }

                            await _hubContext.Clients.All.SendAsync("ReceiveTokenUpdate", appointment.TokenNumber, "Cancelled");
                        }
                        else if (updatedAppointment.Status == "Completed" && appointment.Status != "Completed")
                        {
                            var queue = await _context.Queues
                                .FirstOrDefaultAsync(q => q.TokenNumber == appointment.TokenNumber);
                            if (queue != null)
                            {
                                queue.Status = "Completed";
                                queue.UpdatedAt = DateTime.Now;
                            }
                        }

                        appointment.Status = updatedAppointment.Status;
                    }
                }

                await _context.SaveChangesAsync();

                await _logger.LogInfoAsync($"Appointment updated: {appointmentId}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        AppointmentId = appointmentId,
                        NewStatus = appointment.Status
                    });

                return Ok(new
                {
                    appointment,
                    message = "Appointment updated successfully"
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating appointment: {appointmentId}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = appointmentId });
                return StatusCode(500, new { message = "An error occurred while updating the appointment." });
            }
        }

        // ============ UPDATE APPOINTMENT STATUS ONLY ============
        // PUT: api/Appointment/update-status/{appointmentId}
        [HttpPut("update-status/{appointmentId}")]
        public async Task<IActionResult> UpdateAppointmentStatus(Guid appointmentId, [FromBody] UpdateStatusDto statusUpdate)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Updating appointment status: {appointmentId}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = appointmentId });

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    await _logger.LogWarningAsync($"Appointment not found for status update: {appointmentId}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, AppointmentId = appointmentId });
                    return NotFound(new { message = "Appointment not found" });
                }

                // Check if appointment can be updated
                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    return BadRequest(new { message = $"Cannot update {appointment.Status} appointment" });
                }

                // Validate status
                var validStatuses = new[] { "Pending", "Completed", "Cancelled" };
                if (!string.IsNullOrEmpty(statusUpdate.Status) && validStatuses.Contains(statusUpdate.Status))
                {
                    var oldStatus = appointment.Status;
                    appointment.Status = statusUpdate.Status;

                    // Update queue if needed
                    if (statusUpdate.Status == "Cancelled" || statusUpdate.Status == "Completed")
                    {
                        var queue = await _context.Queues
                            .FirstOrDefaultAsync(q => q.TokenNumber == appointment.TokenNumber);
                        if (queue != null)
                        {
                            queue.Status = statusUpdate.Status;
                            queue.UpdatedAt = DateTime.Now;
                        }
                    }

                    await _context.SaveChangesAsync();

                    await _logger.LogInfoAsync($"Appointment status updated: {appointmentId}, {oldStatus} -> {statusUpdate.Status}",
                        LogCategory.Appointments,
                        new
                        {
                            CorrelationId = correlationId,
                            AppointmentId = appointmentId,
                            OldStatus = oldStatus,
                            NewStatus = statusUpdate.Status
                        });

                    // Send real-time notification
                    await _hubContext.Clients.All.SendAsync("ReceiveTokenUpdate", appointment.TokenNumber, statusUpdate.Status);

                    return Ok(new
                    {
                        appointment,
                        message = $"Appointment status updated to {statusUpdate.Status}"
                    });
                }

                return BadRequest(new { message = "Invalid status value. Valid values: Pending, Completed, Cancelled" });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating appointment status: {appointmentId}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = appointmentId });
                return StatusCode(500, new { message = "An error occurred while updating the appointment status." });
            }
        }

        // PUT: api/Appointment/queue/update/{tokenNumber}
        [HttpPut("queue/update/{tokenNumber}")]
        public async Task<IActionResult> UpdateQueueStatus(string tokenNumber, [FromBody] string status)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Updating queue status for token: {tokenNumber} to {status}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, TokenNumber = tokenNumber, NewStatus = status });

                var queue = await _context.Queues
                    .FirstOrDefaultAsync(q => q.TokenNumber == tokenNumber);

                if (queue == null)
                {
                    await _logger.LogWarningAsync($"Queue entry not found for token: {tokenNumber}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, TokenNumber = tokenNumber });
                    return NotFound("Queue entry not found.");
                }

                var oldStatus = queue.Status;
                queue.Status = status;
                queue.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await _logger.LogInfoAsync($"Queue status changed - Queue ID: {queue.QueueId}, Token: {tokenNumber}, Status: {oldStatus} -> {status}",
                    LogCategory.Appointments,
                    new { QueueId = queue.QueueId, TokenNumber = tokenNumber, OldStatus = oldStatus, NewStatus = status });

                // Update appointment status if needed
                if (status == "Completed" || status == "Cancelled")
                {
                    var token = await _context.Tokens
                        .FirstOrDefaultAsync(t => t.TokenNumber == tokenNumber);

                    if (token != null)
                    {
                        var appointment = await _context.Appointments
                            .FirstOrDefaultAsync(a => a.AppointmentId == token.AppointmentId);

                        if (appointment != null)
                        {
                            appointment.Status = status == "Completed" ? "Completed" : "Cancelled";
                            await _context.SaveChangesAsync();

                            await _logger.LogAppointmentAsync(appointment.AppointmentId, queue.CitizenName, $"marked as {status}");
                        }
                    }
                }

                await _logger.LogInfoAsync($"Queue status updated for token {tokenNumber}: {oldStatus} -> {status}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        TokenNumber = tokenNumber,
                        OldStatus = oldStatus,
                        NewStatus = status,
                        QueueId = queue.QueueId
                    });

                await _hubContext.Clients.All.SendAsync("ReceiveTokenUpdate", tokenNumber, status);

                return Ok(new
                {
                    queue,
                    message = $"Token {tokenNumber} status updated to {status}",
                    oldStatus = oldStatus,
                    newStatus = status
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error updating queue status for token: {tokenNumber}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, TokenNumber = tokenNumber });
                return StatusCode(500, new { message = "An error occurred while updating the queue status." });
            }
        }

        // GET: api/Appointment/queue/statistics/{wardNumber}
        [HttpGet("queue/statistics/{wardNumber}")]
        public async Task<IActionResult> GetQueueStatistics(int wardNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching queue statistics for ward: {wardNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });

                var inQueue = await _context.Queues
                    .CountAsync(q => q.WardNumber == wardNumber && q.Status == "In Queue");

                var inProgress = await _context.Queues
                    .CountAsync(q => q.WardNumber == wardNumber && q.Status == "In Progress");

                var completed = await _context.Queues
                    .CountAsync(q => q.WardNumber == wardNumber && q.Status == "Completed");

                var cancelled = await _context.Queues
                    .CountAsync(q => q.WardNumber == wardNumber && q.Status == "Cancelled");

                var averageWaitTime = await CalculateAverageWaitTime(wardNumber);

                var statistics = new
                {
                    WardNumber = wardNumber,
                    InQueue = inQueue,
                    InProgress = inProgress,
                    Completed = completed,
                    Cancelled = cancelled,
                    Total = inQueue + inProgress + completed + cancelled,
                    AverageWaitTimeMinutes = averageWaitTime,
                    CurrentQueuePosition = inQueue + 1
                };

                await _logger.LogInfoAsync($"Queue statistics retrieved for ward {wardNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber, Statistics = statistics });

                return Ok(statistics);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching queue statistics for ward: {wardNumber}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });
                return StatusCode(500, new { message = "An error occurred while fetching queue statistics." });
            }
        }

        // GET: api/Appointment/appointments/ward/{wardNumber}
        [HttpGet("appointments/ward/{wardNumber}")]
        public async Task<IActionResult> GetAppointmentsByWard(int wardNumber)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Fetching appointments for ward: {wardNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });

                var appointments = await _context.Appointments
                    .Where(a => a.WardNumber == wardNumber)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();

                await _logger.LogInfoAsync($"Retrieved {appointments.Count} appointments for ward {wardNumber}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber, Count = appointments.Count });

                return Ok(appointments);
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error fetching appointments for ward: {wardNumber}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, WardNumber = wardNumber });
                return StatusCode(500, new { message = "An error occurred while fetching appointments." });
            }
        }

        // DELETE: api/Appointment/cancel/{appointmentId}
        [HttpDelete("cancel/{appointmentId}")]
        public async Task<IActionResult> CancelAppointment(Guid appointmentId)
        {
            var correlationId = Guid.NewGuid().ToString();

            try
            {
                await _logger.LogInfoAsync($"Cancelling appointment: {appointmentId}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = appointmentId });

                var appointment = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

                if (appointment == null)
                {
                    await _logger.LogWarningAsync($"Appointment not found for cancellation: {appointmentId}",
                        LogCategory.Appointments,
                        new { CorrelationId = correlationId, AppointmentId = appointmentId });
                    return NotFound(new { message = "Appointment not found" });
                }

                // Check if already cancelled or completed
                if (appointment.Status == "Completed" || appointment.Status == "Cancelled")
                {
                    return BadRequest(new { message = $"Cannot cancel {appointment.Status} appointment" });
                }

                var oldStatus = appointment.Status;
                appointment.Status = "Cancelled";

                var queue = await _context.Queues
                    .FirstOrDefaultAsync(q => q.TokenNumber == appointment.TokenNumber);

                if (queue != null)
                {
                    var oldQueueStatus = queue.Status;
                    queue.Status = "Cancelled";
                    queue.UpdatedAt = DateTime.Now;

                    await _logger.LogInfoAsync($"Queue status updated for cancelled appointment - Queue ID: {queue.QueueId}, Status: {oldQueueStatus} -> Cancelled",
                        LogCategory.Appointments,
                        new { QueueId = queue.QueueId, TokenNumber = appointment.TokenNumber });
                }

                await _context.SaveChangesAsync();

                await _logger.LogWarningAsync($"Appointment cancelled: {appointmentId}, Token: {appointment.TokenNumber}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        AppointmentId = appointmentId,
                        TokenNumber = appointment.TokenNumber,
                        CitizenName = appointment.CitizenName,
                        OldStatus = oldStatus
                    });

                await _hubContext.Clients.All.SendAsync("ReceiveTokenUpdate", appointment.TokenNumber, "Cancelled");

                return Ok(new
                {
                    message = "Appointment cancelled successfully",
                    appointmentId = appointmentId,
                    tokenNumber = appointment.TokenNumber
                });
            }
            catch (Exception ex)
            {
                await _logger.LogErrorAsync($"Error cancelling appointment: {appointmentId}",
                    ex,
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = appointmentId });
                return StatusCode(500, new { message = "An error occurred while cancelling the appointment." });
            }
        }

        // Helper method to calculate average wait time
        private async Task<double> CalculateAverageWaitTime(int wardNumber)
        {
            try
            {
                var completedQueues = await _context.Queues
                    .Where(q => q.WardNumber == wardNumber && q.Status == "Completed")
                    .ToListAsync();

                if (!completedQueues.Any())
                    return 0;

                var waitTimes = completedQueues
                    .Select(q => (q.UpdatedAt - q.CreatedAt).TotalMinutes)
                    .ToList();

                return Math.Round(waitTimes.Average(), 2);
            }
            catch
            {
                return 0;
            }
        }
    }
}