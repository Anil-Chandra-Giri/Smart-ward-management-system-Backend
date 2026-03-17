using Smart_ward_management_system.Model.WasteManagement_And_Scheduling;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Smart_ward_management_system.DTOs.WasteCollectionDTOs
{
    public class WasteCollectionRouteDto
    {
        public Guid Id { get; set; }
        public string RouteName { get; set; }
        public string WasteType { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Status { get; set; }
        public Guid AssignedVehicleId { get; set; }
        public string VehicleName { get; set; }
        public string VehicleNumber { get; set; }
        public Guid AssignedDriverId { get; set; }
        public string DriverName { get; set; }
        public string DriverPhone { get; set; }
        public string Description { get; set; }
        public double EstimatedDistance { get; set; }
        public int EstimatedDuration { get; set; }
        public DateTime CreatedAt { get; set; }

        // Collection points without back reference to route
        public List<CollectionPointDto> CollectionPoints { get; set; }
    }

    // DTO for collection point without circular reference
    public class CollectionPointDto
    {
        public Guid Id { get; set; }
        public string Address { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int SequenceOrder { get; set; }
        public DateTime? ActualCollectionTime { get; set; }
        public double WasteQuantity { get; set; }
        public string Notes { get; set; }
        public Guid? CollectedBy { get; set; } // Add this if needed


        // Don't include the Route property here to break the cycle
    }

    // DTO for creating a route
    public class CreateRouteDto
    {
        [Required]
        public string RouteName { get; set; }

        [Required]
        [JsonConverter(typeof(JsonStringEnumConverter))] // Add this to handle both string and number
        public WasteType WasteType { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        [Required]
        public Guid AssignedVehicleId { get; set; }

        [Required]
        public Guid AssignedDriverId { get; set; }

        public string Description { get; set; }

        public List<CreateCollectionPointDto> CollectionPoints { get; set; }
    }


    public class CreateCollectionPointDto
    {
        [Required]
        public string Address { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int SequenceOrder { get; set; }

        public string Notes { get; set; }
    }


    public class VehicleDto
    {
        public Guid Id { get; set; }
        public string VehicleNumber { get; set; }
        public string VehicleName { get; set; }
        public string Status { get; set; }
        public double Capacity { get; set; }
        public string VehicleType { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime LastUpdatedLocation { get; set; }
    }

    public class UpdateVehicleLocationDto
    {
        [Required]
        public Guid VehicleId { get; set; }

        [Required]
        [Range(-90, 90)]
        public double Latitude { get; set; }

        [Required]
        [Range(-180, 180)]
        public double Longitude { get; set; }
    }

    public class RouteStatusUpdateDto
    {
        [Required]
        public Guid RouteId { get; set; }

        [Required]
        public RouteStatus Status { get; set; }

        public string DelayReason { get; set; }

        public int? DelayMinutes { get; set; }
    }

    public class ScheduleDto
    {
        public Guid Id { get; set; }
        public string RouteName { get; set; }
        public DateTime ScheduledStartTime { get; set; }
        public DateTime ScheduledEndTime { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public string Status { get; set; }
        public string DelayReason { get; set; }
    }

    public class WeeklyScheduleDto
    {
        public DateTime WeekStartDate { get; set; }
        public DateTime WeekEndDate { get; set; }
        public List<DailyScheduleDto> DailySchedules { get; set; }
    }

    public class DailyScheduleDto
    {
        public DateTime Date { get; set; }
        public string DayOfWeek { get; set; }
        public List<ScheduleDto> Routes { get; set; }
    }
}
