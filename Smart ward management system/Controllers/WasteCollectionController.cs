using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.DTOs.WasteCollectionDTOs;
using Smart_ward_management_system.Model.WasteManagement_And_Scheduling;

namespace Smart_ward_management_system.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WasteCollectionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WasteCollectionController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/WasteCollection/routes/weekly?startDate=2024-01-01
        [HttpGet("routes/weekly")]
        public async Task<ActionResult<WeeklyScheduleDto>> GetWeeklySchedule(DateTime startDate)
        {
            var endDate = startDate.AddDays(7);

            var routes = await _context.WasteCollectionRoutes
                .Include(r => r.AssignedVehicle)
                .Include(r => r.AssignedDriver)
                .Include(r => r.CollectionPoints)
                .Where(r => r.ScheduledDate >= startDate && r.ScheduledDate < endDate)
                .OrderBy(r => r.ScheduledDate)
                .ToListAsync();

            var weeklySchedule = new WeeklyScheduleDto
            {
                WeekStartDate = startDate,
                WeekEndDate = endDate,
                DailySchedules = new List<DailyScheduleDto>()
            };

            for (int i = 0; i < 7; i++)
            {
                var currentDate = startDate.AddDays(i);
                var dailyRoutes = routes.Where(r => r.ScheduledDate.Date == currentDate.Date)
                    .Select(r => new ScheduleDto
                    {
                        Id = r.Id,
                        RouteName = r.RouteName,
                        ScheduledStartTime = r.ScheduledDate,
                        ScheduledEndTime = r.ScheduledDate.AddMinutes(r.EstimatedDuration),
                        ActualStartTime = r.StartTime,
                        ActualEndTime = r.EndTime,
                        Status = r.Status.ToString(),
                        DelayReason = null
                    }).ToList();

                weeklySchedule.DailySchedules.Add(new DailyScheduleDto
                {
                    Date = currentDate,
                    DayOfWeek = currentDate.DayOfWeek.ToString(),
                    Routes = dailyRoutes
                });
            }

            return Ok(weeklySchedule);
        }

        // GET: api/WasteCollection/routes/monthly?year=2024&month=1
        [HttpGet("routes/monthly")]
        public async Task<ActionResult<IEnumerable<WasteCollectionRouteDto>>> GetMonthlySchedule(int year, int month)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var routes = await _context.WasteCollectionRoutes
                .Include(r => r.AssignedVehicle)
                .Include(r => r.AssignedDriver)
                .Include(r => r.CollectionPoints)
                .Where(r => r.ScheduledDate >= startDate && r.ScheduledDate < endDate)
                .OrderBy(r => r.ScheduledDate)
                .ToListAsync();

            var routeDtos = routes.Select(r => new WasteCollectionRouteDto
            {
                Id = r.Id,
                RouteName = r.RouteName,
                WasteType = r.WasteType.ToString(),
                ScheduledDate = r.ScheduledDate,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Status = r.Status.ToString(),
                AssignedVehicleId = r.AssignedVehicleId,
                VehicleName = r.AssignedVehicle?.VehicleName,
                AssignedDriverId = r.AssignedDriverId,
                DriverName = r.AssignedDriver?.Name,
                Description = r.Description,
                EstimatedDistance = r.EstimatedDistance,
                EstimatedDuration = r.EstimatedDuration,
                CollectionPoints = r.CollectionPoints?.Select(cp => new CollectionPointDto
                {
                    Id = cp.Id,
                    Address = cp.Address,
                    Latitude = cp.Latitude,
                    Longitude = cp.Longitude,
                    SequenceOrder = cp.SequenceOrder,
                    ActualCollectionTime = cp.ActualCollectionTime,
                    WasteQuantity = cp.WasteQuantity
                }).OrderBy(cp => cp.SequenceOrder).ToList()
            }).ToList();

            return Ok(routeDtos);
        }

        // POST: api/WasteCollection/routes
        // POST: api/WasteCollection/routes
        [HttpPost("routes")]
        public async Task<ActionResult<WasteCollectionRouteDto>> CreateRoute(CreateRouteDto createRouteDto) // Changed return type
        {
            try
            {
                // Check if vehicle exists
                var vehicle = await _context.WasteVehicles.FindAsync(createRouteDto.AssignedVehicleId);
                if (vehicle == null)
                    return BadRequest(new { message = "Vehicle not found" });

                // Check if driver exists
                var driver = await _context.Drivers.FindAsync(createRouteDto.AssignedDriverId);
                if (driver == null)
                    return BadRequest(new { message = "Driver not found" });

                var route = new WasteCollectionRoute
                {
                    Id = Guid.NewGuid(),
                    RouteName = createRouteDto.RouteName,
                    WasteType = createRouteDto.WasteType,
                    ScheduledDate = createRouteDto.ScheduledDate,
                    AssignedVehicleId = createRouteDto.AssignedVehicleId,
                    AssignedDriverId = createRouteDto.AssignedDriverId,
                    Description = createRouteDto.Description,
                    Status = RouteStatus.Planned,
                    CreatedAt = DateTime.UtcNow,
                    CollectionPoints = new List<CollectionPoint>()
                };

                if (createRouteDto.CollectionPoints != null)
                {
                    foreach (var cp in createRouteDto.CollectionPoints)
                    {
                        route.CollectionPoints.Add(new CollectionPoint
                        {
                            Id = Guid.NewGuid(),
                            Address = cp.Address,
                            Latitude = cp.Latitude,
                            Longitude = cp.Longitude,
                            SequenceOrder = cp.SequenceOrder,
                            Notes = cp.Notes // Include Notes
                        });
                    }
                }

                // Calculate estimated distance and duration
                route.EstimatedDistance = route.CollectionPoints.Count * 2.5;
                route.EstimatedDuration = route.CollectionPoints.Count * 15;

                _context.WasteCollectionRoutes.Add(route);
                await _context.SaveChangesAsync();

                // Return DTO instead of entity
                var routeDto = new WasteCollectionRouteDto
                {
                    Id = route.Id,
                    RouteName = route.RouteName,
                    WasteType = route.WasteType.ToString(),
                    ScheduledDate = route.ScheduledDate,
                    Status = route.Status.ToString(),
                    AssignedVehicleId = route.AssignedVehicleId,
                    VehicleName = vehicle.VehicleName,
                    VehicleNumber = vehicle.VehicleNumber,
                    AssignedDriverId = route.AssignedDriverId,
                    DriverName = driver.Name,
                    DriverPhone = driver.PhoneNumber,
                    Description = route.Description,
                    EstimatedDistance = route.EstimatedDistance,
                    EstimatedDuration = route.EstimatedDuration,
                    CreatedAt = route.CreatedAt,
                    CollectionPoints = route.CollectionPoints?.Select(cp => new CollectionPointDto
                    {
                        Id = cp.Id,
                        Address = cp.Address,
                        Latitude = cp.Latitude,
                        Longitude = cp.Longitude,
                        SequenceOrder = cp.SequenceOrder,
                        Notes = cp.Notes
                    }).ToList()
                };

                return CreatedAtAction(nameof(GetRoute), new { id = route.Id }, routeDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/WasteCollection/routes/5
        [HttpGet("routes/{id}")]
        public async Task<ActionResult<WasteCollectionRouteDto>> GetRoute(Guid id)
        {
            var route = await _context.WasteCollectionRoutes
                .Include(r => r.AssignedVehicle)
                .Include(r => r.AssignedDriver)
                .Include(r => r.CollectionPoints)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null)
            {
                return NotFound();
            }

            var routeDto = new WasteCollectionRouteDto
            {
                Id = route.Id,
                RouteName = route.RouteName,
                WasteType = route.WasteType.ToString(),
                ScheduledDate = route.ScheduledDate,
                StartTime = route.StartTime,
                EndTime = route.EndTime,
                Status = route.Status.ToString(),
                AssignedVehicleId = route.AssignedVehicleId,
                VehicleName = route.AssignedVehicle?.VehicleName,
                AssignedDriverId = route.AssignedDriverId,
                DriverName = route.AssignedDriver?.Name,
                Description = route.Description,
                EstimatedDistance = route.EstimatedDistance,
                EstimatedDuration = route.EstimatedDuration,
                CollectionPoints = route.CollectionPoints?.Select(cp => new CollectionPointDto
                {
                    Id = cp.Id,
                    Address = cp.Address,
                    Latitude = cp.Latitude,
                    Longitude = cp.Longitude,
                    SequenceOrder = cp.SequenceOrder,
                    ActualCollectionTime = cp.ActualCollectionTime,
                    WasteQuantity = cp.WasteQuantity
                }).OrderBy(cp => cp.SequenceOrder).ToList()
            };

            return Ok(routeDto);
        }

        // PUT: api/WasteCollection/routes/5/status
        [HttpPut("routes/{id}/status")]
        public async Task<IActionResult> UpdateRouteStatus(Guid id, RouteStatusUpdateDto statusUpdate)
        {
            var route = await _context.WasteCollectionRoutes
                .Include(r => r.CollectionPoints)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (route == null)
                return NotFound();

            // Validate status transition
            if (!IsValidStatusTransition(route.Status, statusUpdate.Status))
                return BadRequest($"Invalid status transition from {route.Status} to {statusUpdate.Status}");

            route.Status = statusUpdate.Status;
            route.UpdatedAt = DateTime.UtcNow;

            // Set timestamps based on status
            switch (statusUpdate.Status)
            {
                case RouteStatus.InProgress:
                    if (route.StartTime == null)
                        route.StartTime = DateTime.UtcNow;
                    break;

                case RouteStatus.Completed:
                    route.EndTime = DateTime.UtcNow;
                    break;

                case RouteStatus.Delayed:
                    // Log delay reason
                    if (!string.IsNullOrEmpty(statusUpdate.DelayReason))
                    {
                        var schedule = new RouteSchedule
                        {
                            Id = Guid.NewGuid(),
                            RouteId = id,
                            ScheduledStartTime = route.ScheduledDate,
                            ScheduledEndTime = route.ScheduledDate.AddMinutes(route.EstimatedDuration),
                            ActualStartTime = route.StartTime,
                            DelayReason = statusUpdate.DelayReason,
                            DelayMinutes = statusUpdate.DelayMinutes ?? 0
                        };
                        _context.RouteSchedules.Add(schedule);
                    }
                    break;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Route status updated to {statusUpdate.Status}",
                routeId = route.Id,
                startTime = route.StartTime,
                endTime = route.EndTime,
                estimatedCompletion = route.StartTime.HasValue
                    ? route.StartTime.Value.AddMinutes(route.EstimatedDuration)
                    : (DateTime?)null
            });
        }

        private bool IsValidStatusTransition(RouteStatus current, RouteStatus next)
        {
            // Define allowed transitions
            return (current, next) switch
            {
                (RouteStatus.Planned, RouteStatus.InProgress) => true,
                (RouteStatus.Planned, RouteStatus.Cancelled) => true,
                (RouteStatus.InProgress, RouteStatus.Completed) => true,
                (RouteStatus.InProgress, RouteStatus.Delayed) => true,
                (RouteStatus.Delayed, RouteStatus.InProgress) => true,
                (RouteStatus.Delayed, RouteStatus.Completed) => true,
                _ => false
            };
        }

        // Controllers/WasteCollectionController.cs

        [HttpPost("collection-points/{pointId}/complete")]
        public async Task<IActionResult> CompleteCollectionPoint(Guid pointId, [FromBody] CompletePointDto completeDto)
        {
            var point = await _context.CollectionPoints
                .Include(p => p.Route)
                .FirstOrDefaultAsync(p => p.Id == pointId);

            if (point == null)
                return NotFound();

            // Update collection point
            point.ActualCollectionTime = DateTime.UtcNow;
            point.WasteQuantity = completeDto.WasteQuantity;
            point.Notes = completeDto.Notes;

            await _context.SaveChangesAsync();

            // Check if route is complete
            var route = point.Route;
            var allPointsCompleted = await _context.CollectionPoints
                .Where(p => p.RouteId == route.Id)
                .AllAsync(p => p.ActualCollectionTime != null);

            if (allPointsCompleted && route.Status != RouteStatus.Completed)
            {
                // Auto-complete the route
                route.Status = RouteStatus.Completed;
                route.EndTime = DateTime.UtcNow;
                route.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Collection point completed. Route automatically completed.",
                    routeCompleted = true,
                    nextPoint = (string)null
                });
            }

            // Get next incomplete point
            var nextPoint = await _context.CollectionPoints
                .Where(p => p.RouteId == route.Id && p.ActualCollectionTime == null)
                .OrderBy(p => p.SequenceOrder)
                .FirstOrDefaultAsync();

            return Ok(new
            {
                message = "Collection point completed successfully",
                routeCompleted = false,
                nextPoint = nextPoint != null ? new
                {
                    nextPoint.Id,
                    nextPoint.Address,
                    nextPoint.SequenceOrder
                } : null
            });
        }

        public class CompletePointDto
        {
            public double WasteQuantity { get; set; }
            public string Notes { get; set; }
        }

        // GET: api/WasteCollection/vehicles
        [HttpGet("vehicles")]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetVehicles()
        {
            var vehicles = await _context.WasteVehicles
                .Where(v => v.IsActive)
                .ToListAsync();

            var vehicleDtos = vehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                VehicleNumber = v.VehicleNumber,
                VehicleName = v.VehicleName,
                Status = v.Status.ToString(),
                Capacity = v.Capacity,
                VehicleType = v.VehicleType,
                Latitude = v.Latitude,
                Longitude = v.Longitude,
                LastUpdatedLocation = v.LastUpdatedLocation
            }).ToList();

            return Ok(vehicleDtos);
        }

        // POST: api/WasteCollection/vehicles/location
        [HttpPost("vehicles/location")]
        public async Task<IActionResult> UpdateVehicleLocation(UpdateVehicleLocationDto locationUpdate)
        {
            var vehicle = await _context.WasteVehicles.FindAsync(locationUpdate.VehicleId);
            if (vehicle == null)
            {
                return NotFound();
            }

            vehicle.Latitude = locationUpdate.Latitude;
            vehicle.Longitude = locationUpdate.Longitude;
            vehicle.LastUpdatedLocation = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // GET: api/WasteCollection/vehicles/available
        [HttpGet("vehicles/available")]
        public async Task<ActionResult<IEnumerable<VehicleDto>>> GetAvailableVehicles(DateTime date)
        {
            var scheduledVehicles = await _context.WasteCollectionRoutes
                .Where(r => r.ScheduledDate.Date == date.Date)
                .Select(r => r.AssignedVehicleId)
                .ToListAsync();

            var availableVehicles = await _context.WasteVehicles
                .Where(v => v.IsActive && v.Status == VehicleStatus.Available && !scheduledVehicles.Contains(v.Id))
                .ToListAsync();

            var vehicleDtos = availableVehicles.Select(v => new VehicleDto
            {
                Id = v.Id,
                VehicleNumber = v.VehicleNumber,
                VehicleName = v.VehicleName,
                Status = v.Status.ToString(),
                Capacity = v.Capacity,
                VehicleType = v.VehicleType
            }).ToList();

            return Ok(vehicleDtos);
        }

        // GET: api/WasteCollection/drivers/available
        [HttpGet("drivers/available")]
        public async Task<ActionResult<IEnumerable<Driver>>> GetAvailableDrivers(DateTime date)
        {
            var scheduledDrivers = await _context.WasteCollectionRoutes
                .Where(r => r.ScheduledDate.Date == date.Date)
                .Select(r => r.AssignedDriverId)
                .ToListAsync();

            var availableDrivers = await _context.Drivers
                .Where(d => d.IsAvailable && !scheduledDrivers.Contains(d.Id))
                .ToListAsync();

            return Ok(availableDrivers);
        }

        // GET: api/WasteCollection/realtime-updates
        [HttpGet("realtime-updates")]
        public async Task<ActionResult<IEnumerable<object>>> GetRealtimeUpdates()
        {
            var activeRoutes = await _context.WasteCollectionRoutes
                .Include(r => r.AssignedVehicle)
                .Include(r => r.AssignedDriver)
                .Include(r => r.CollectionPoints)
                .Where(r => r.Status == RouteStatus.InProgress)
                .Select(r => new
                {
                    RouteId = r.Id,
                    RouteName = r.RouteName,
                    VehicleNumber = r.AssignedVehicle.VehicleNumber,
                    DriverName = r.AssignedDriver.Name,
                    Status = r.Status.ToString(),
                    StartTime = r.StartTime,
                    EstimatedCompletion = r.StartTime.HasValue
                        ? r.StartTime.Value.AddMinutes(r.EstimatedDuration)
                        : (DateTime?)null,
                    ElapsedMinutes = r.StartTime.HasValue
                        ? (int?)(DateTime.UtcNow - r.StartTime.Value).TotalMinutes
                        : null,
                    CollectionPoints = r.CollectionPoints
                        .OrderBy(cp => cp.SequenceOrder)
                        .Select(cp => new
                        {
                            cp.Address,
                            cp.SequenceOrder,
                            cp.ActualCollectionTime,
                            IsCompleted = cp.ActualCollectionTime.HasValue
                        }),
                    CompletedPoints = r.CollectionPoints.Count(cp => cp.ActualCollectionTime.HasValue),
                    TotalPoints = r.CollectionPoints.Count(),
                    ProgressPercentage = r.CollectionPoints.Count() > 0
                        ? (int)((double)r.CollectionPoints.Count(cp => cp.ActualCollectionTime.HasValue)
                            / r.CollectionPoints.Count() * 100)
                        : 0
                })
                .ToListAsync();

            return Ok(activeRoutes);
        }
    }
}
