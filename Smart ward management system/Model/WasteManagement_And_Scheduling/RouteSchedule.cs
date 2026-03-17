using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.WasteManagement_And_Scheduling
{
    public class RouteSchedule
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RouteId { get; set; }

        public DateTime ScheduledStartTime { get; set; }

        public DateTime ScheduledEndTime { get; set; }

        public DateTime? ActualStartTime { get; set; }

        public DateTime? ActualEndTime { get; set; }

        public string DelayReason { get; set; }

        public int DelayMinutes { get; set; }

        [ForeignKey("RouteId")]
        public virtual WasteCollectionRoute Route { get; set; }
    }
}
