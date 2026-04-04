using Microsoft.EntityFrameworkCore;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Model.Common;

namespace Smart_ward_management_system.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(ApplicationDbContext db, ILogger<NotificationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task SendNotification(Guid userId, string title, string message)
        {
            try
            {
                var notification = new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    Type = NotificationType.Info
                };

                _db.UserNotifications.Add(notification);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Notification sent to user {userId}: {title}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to user {userId}");
                throw;
            }
        }

        public async Task SendNotificationToRole(string role, string title, string message)
        {
            try
            {
                // Get all users with the specified role
                var users = await _db.Users
                    .Where(u => u.Role == role)
                    .Select(u => u.UserId)
                    .ToListAsync();

                var notifications = users.Select(userId => new UserNotification
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Title = title,
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    Type = NotificationType.Info
                }).ToList();

                await _db.UserNotifications.AddRangeAsync(notifications);
                await _db.SaveChangesAsync();

                _logger.LogInformation($"Notification sent to {users.Count} users with role {role}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send notification to role {role}");
                throw;
            }
        }

        public async Task<List<UserNotification>> GetUserNotifications(Guid userId, bool unreadOnly = false)
        {
            try
            {
                var query = _db.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsDeleted);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                return await query
                    .OrderByDescending(n => n.CreatedAt)
                    .Take(100)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get notifications for user {userId}");
                throw;
            }
        }

        public async Task MarkAsRead(Guid notificationId)
        {
            try
            {
                var notification = await _db.UserNotifications.FindAsync(notificationId);
                if (notification != null && !notification.IsRead)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to mark notification {notificationId} as read");
                throw;
            }
        }

        public async Task MarkAllAsRead(Guid userId)
        {
            try
            {
                var unreadNotifications = await _db.UserNotifications
                    .Where(n => n.UserId == userId && !n.IsRead && !n.IsDeleted)
                    .ToListAsync();

                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    notification.ReadAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation($"Marked {unreadNotifications.Count} notifications as read for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to mark all notifications as read for user {userId}");
                throw;
            }
        }

        public async Task<int> GetUnreadCount(Guid userId)
        {
            try
            {
                return await _db.UserNotifications
                    .CountAsync(n => n.UserId == userId && !n.IsRead && !n.IsDeleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get unread count for user {userId}");
                throw;
            }
        }

        public async Task DeleteNotification(Guid notificationId)
        {
            try
            {
                var notification = await _db.UserNotifications.FindAsync(notificationId);
                if (notification != null)
                {
                    notification.IsDeleted = true;
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete notification {notificationId}");
                throw;
            }
        }

        public async Task DeleteOldNotifications(int daysOld = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysOld);
                var oldNotifications = await _db.UserNotifications
                    .Where(n => n.CreatedAt < cutoffDate && n.IsRead)
                    .ToListAsync();

                foreach (var notification in oldNotifications)
                {
                    notification.IsDeleted = true;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation($"Deleted {oldNotifications.Count} old notifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete old notifications");
                throw;
            }
        }
    }
}
