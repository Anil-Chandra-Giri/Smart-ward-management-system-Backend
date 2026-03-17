using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.WasteManagement_And_Scheduling
{
    public class Driver
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(20)]
        public string LicenseNumber { get; set; }

        [Required]
        [StringLength(15)]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public bool IsAvailable { get; set; }

        public DateTime? AssignedRouteDate { get; set; }

        public virtual ICollection<WasteCollectionRoute> Routes { get; set; }
    }
}
