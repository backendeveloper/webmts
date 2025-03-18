using NotificationService.Data.Entities;

namespace NotificationService.Data.Repositories.Imp;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate>
{
    Task<NotificationTemplate> GetTemplateByNameAsync(string templateName);
}