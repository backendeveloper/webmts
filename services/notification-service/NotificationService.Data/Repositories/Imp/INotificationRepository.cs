using NotificationService.Data.Entities;

namespace NotificationService.Data.Repositories.Imp;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int maxCount = 10);
    Task<IEnumerable<Notification>> GetNotificationsByRelatedEntityAsync(string entityId, string entityType);
}