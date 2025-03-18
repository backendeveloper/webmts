using Microsoft.EntityFrameworkCore;
using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;
using NotificationService.Data.Repositories.Imp;

namespace NotificationService.Data.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(NotificationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetPendingNotificationsAsync(int maxCount = 10)
    {
        return await _context.Notifications
            .Where(n => n.Status == NotificationStatusType.Pending)
            .OrderBy(n => n.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task<IEnumerable<Notification>> GetNotificationsByRelatedEntityAsync(string entityId,
        string entityType)
    {
        return await _context.Notifications
            .Where(n => n.RelatedEntityId == entityId && n.RelatedEntityType == entityType)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }
}