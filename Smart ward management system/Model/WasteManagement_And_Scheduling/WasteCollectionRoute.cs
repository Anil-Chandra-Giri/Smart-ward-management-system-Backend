using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.WasteManagement_And_Scheduling
{
    public class WasteCollectionRoute
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string RouteName { get; set; }

        [Required]
        public WasteType WasteType { get; set; }

        [Required]
        public DateTime ScheduledDate { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        [Required]
        public RouteStatus Status { get; set; }

        [Required]
        public Guid AssignedVehicleId { get; set; }

        [Required]
        public Guid AssignedDriverId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public string? Waypoints { get; set; } // JSON string for route waypoints

        public double EstimatedDistance { get; set; }

        public int EstimatedDuration { get; set; } // in minutes

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("AssignedVehicleId")]
        public virtual WasteVehicle AssignedVehicle { get; set; }

        [ForeignKey("AssignedDriverId")]
        public virtual Driver AssignedDriver { get; set; }

        public virtual ICollection<CollectionPoint> CollectionPoints { get; set; }
    }
}
