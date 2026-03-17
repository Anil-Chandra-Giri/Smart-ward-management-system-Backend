using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.WasteManagement_And_Scheduling
{
    public class WasteVehicle
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(50)]
        public string VehicleNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string VehicleName { get; set; }

        [Required]
        public VehicleStatus Status { get; set; }

        public double Capacity { get; set; } // in tons

        [StringLength(50)]
        public string VehicleType { get; set; }

        public DateTime? LastMaintenanceDate { get; set; }

        public DateTime? NextMaintenanceDate { get; set; }

        public double CurrentFuelLevel { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public DateTime LastUpdatedLocation { get; set; }

        public bool IsActive { get; set; }

        public virtual ICollection<WasteCollectionRoute> Routes { get; set; }
    }
}
