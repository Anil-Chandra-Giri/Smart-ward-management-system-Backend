using System.ComponentModel.DataAnnotations;

namespace Smart_ward_management_system.Model.Services.Complaints
{
    public class StatusMaster
    {
      [Key]  public Guid StatusId { get; set; }
        public string StatusCode { get; set; }
        public string StatusName { get; set; }
        public string Description { get; set; }
    }
}
