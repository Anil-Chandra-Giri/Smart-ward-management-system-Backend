using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Smart_ward_management_system.Model.Volunteer
{
    public class ResourceDistribution
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [ForeignKey("Resource")]
        public Guid ResourceId { get; set; }

        [ForeignKey("DisasterEvent")]
        public Guid? DisasterEventId { get; set; }

        public int Quantity { get; set; }

        public DateTime DistributionDate { get; set; } = DateTime.UtcNow;

        public string RecipientType { get; set; } // Individual, Family, Shelter, etc.

        public string RecipientName { get; set; }

        public string RecipientContact { get; set; }

        public string DistributionLocation { get; set; }

        [ForeignKey("DistributedBy")]
        public Guid? DistributedByVolunteerId { get; set; }

        public string Notes { get; set; }

        // Navigation properties
        public virtual Resource Resource { get; set; }
        public virtual DisasterEvent DisasterEvent { get; set; }
        public virtual Volunteer DistributedBy { get; set; }
    }
}
