using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.WasteManagement_And_Scheduling
{
    public class CollectionPoint
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RouteId { get; set; }

        [Required]
        [StringLength(200)]
        public string Address { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public int SequenceOrder { get; set; }

        public DateTime? ActualCollectionTime { get; set; }

        public double WasteQuantity { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [ForeignKey("RouteId")]
        public virtual WasteCollectionRoute Route { get; set; }
    }
}
