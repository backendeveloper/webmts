using NotificationService.Data.Repositories.Imp;

namespace NotificationService.Data;

public interface IUnitOfWork : IDisposable
{
    INotificationTemplateRepository NotificationTemplates { get; }
    INotificationRepository Notifications { get; }

    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}