namespace Smart_ward_management_system.Model.Common
{
    public class Notification
    {
        public Guid NotificationId { get; set; }
        public Guid UserId { get; set; }
        public string NotificationType { get; set; }
        public string Message { get; set; }
        public bool IsDelivered { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
}
