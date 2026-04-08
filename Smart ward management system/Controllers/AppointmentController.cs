using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs;
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

        // GET: api/<AppointmentController>/queue/{wardNumber}
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

        [Route("GetAllAppointments")]
        [HttpGet]
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

        //// GET api/<AppointmentController>/appointment/{id}
        //[HttpGet("appointment/{id}")]
        //public async Task<IActionResult> GetAppointmentById(Guid id)

        // GET api/<AppointmentController>/5
        [Route("MyAppointments")]
        [HttpGet]
        public async Task<IActionResult> GetMyAppointments(Guid id)
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
            var myAppointments =  await _context.Appointments.Where(s=>s.UserId == id).ToListAsync();
            return Ok(myAppointments);
        

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

        // GET api/<AppointmentController>/token/{tokenNumber}
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

        // POST api/<AppointmentController>/book

        // POST api/<AppointmentController>
        [HttpPost("book")]
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

                await _logger.LogInfoAsync($"Booking appointment for citizen: {appointment.CitizenName}, Ward: {appointment.WardNumber}, Service: {appointment.ServiceType}",
                    LogCategory.Appointments,
                    new
                    {
                        CorrelationId = correlationId,
                        CitizenName = appointment.CitizenName,
                        WardNumber = appointment.WardNumber,
                        ServiceType = appointment.ServiceType,
                        ContactNumber = appointment.ContactNumber
                    });

                var newAppointment = new Appointment
                {
                    AppointmentId = Guid.NewGuid(),
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

                await _logger.LogInfoAsync($"Appointment created with ID: {newAppointment.AppointmentId}",
                    LogCategory.Appointments,
                    new { CorrelationId = correlationId, AppointmentId = newAppointment.AppointmentId });

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

                // Log the appointment booking - using string for queue ID since it's Guid
                await _logger.LogAppointmentAsync(newAppointment.AppointmentId, appointment.CitizenName, "booked");

                // Log queue action - using queue.QueueId as string
                await _logger.LogInfoAsync($"Queue entry created - Queue ID: {queue.QueueId}, Token: {tokenNumber}, Status: In Queue",
                    LogCategory.Appointments,
                    new { QueueId = queue.QueueId, TokenNumber = tokenNumber });

                // Log citizen action
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
                        QueueId = queue.QueueId
                    });

                // Send real-time notification via SignalR
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
                        ErrorMessage = ex.Message
                    });
                return StatusCode(500, new { message = "An error occurred while booking the appointment." });
            }
        }

        // PUT api/<AppointmentController>/queue/update/{tokenNumber}
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

                // Log the queue status change using LogInfoAsync instead
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

                // Send real-time notification via SignalR
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

        // GET: api/<AppointmentController>/GetQueueStatistics
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

        // GET: api/<AppointmentController>/GetAppointmentsByWard
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

        // DELETE api/<AppointmentController>/cancel/{appointmentId}
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

                return Ok(new { message = "Appointment cancelled successfully", appointmentId = appointmentId });
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