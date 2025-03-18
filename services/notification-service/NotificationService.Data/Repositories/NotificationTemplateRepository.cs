using Microsoft.EntityFrameworkCore;
using NotificationService.Data.Entities;
using NotificationService.Data.Repositories.Imp;

namespace NotificationService.Data.Repositories;

public class NotificationTemplateRepository : Repository<NotificationTemplate>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(NotificationDbContext context) : base(context)
    {
    }

    public async Task<NotificationTemplate> GetTemplateByNameAsync(string templateName)
    {
        return await _context.NotificationTemplates
            .Where(t => t.Name == templateName && t.IsActive)
            .FirstOrDefaultAsync();
    }
}