using Smart_ward_management_system.Model.Common;

namespace Smart_ward_management_system.Services
{
    public interface INotificationService
    {
        Task SendNotification(Guid userId, string title, string message);
        Task SendNotificationToRole(string role, string title, string message);
        Task<List<UserNotification>> GetUserNotifications(Guid userId, bool unreadOnly = false);
        Task MarkAsRead(Guid notificationId);
        Task MarkAllAsRead(Guid userId);
        Task<int> GetUnreadCount(Guid userId);
        Task DeleteNotification(Guid notificationId);
        Task DeleteOldNotifications(int daysOld = 30);
    }
}
