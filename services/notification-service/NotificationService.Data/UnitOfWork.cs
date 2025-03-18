using Microsoft.EntityFrameworkCore.Storage;
using NotificationService.Data.Repositories;
using NotificationService.Data.Repositories.Imp;

namespace NotificationService.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationDbContext _context;
    private IDbContextTransaction _transaction;
    private bool _disposed = false;

    private INotificationTemplateRepository _notificationTemplateRepository;
    private INotificationRepository _notificationRepository;

    public UnitOfWork(NotificationDbContext context)
    {
        _context = context;
    }

    public INotificationTemplateRepository NotificationTemplates =>
        _notificationTemplateRepository ??= new NotificationTemplateRepository(_context);

    public INotificationRepository Notifications =>
        _notificationRepository ??= new NotificationRepository(_context);

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _transaction?.Dispose();
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}