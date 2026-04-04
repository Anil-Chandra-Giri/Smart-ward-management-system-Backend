using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Volunteer
{
    public class Resource
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        public string Type { get; set; } // Food, Medical, Equipment, etc.

        public string Category { get; set; }

        public string Description { get; set; }

        public int Quantity { get; set; }

        public int MinimumThreshold { get; set; }

        public string Unit { get; set; } // kg, liters, pieces, boxes

        public DateTime? ExpiryDate { get; set; }

        public string StorageLocation { get; set; }

        public string Supplier { get; set; }

        public decimal? UnitPrice { get; set; }

        public string Status { get; set; } // Available, Low Stock, Out of Stock

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ICollection<ResourceDistribution> Distributions { get; set; }
    }
}
