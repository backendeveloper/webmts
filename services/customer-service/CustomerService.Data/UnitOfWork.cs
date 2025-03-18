using CustomerService.Data.Repositories;
using CustomerService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore.Storage;

namespace CustomerService.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly CustomerDbContext _context;
    private IDbContextTransaction _transaction;
    private bool _disposed = false;

    private ICustomerRepository _userRepository;

    public UnitOfWork(CustomerDbContext context)
    {
        _context = context;
    }

    public ICustomerRepository Customers => _userRepository ??= new CustomerRepository(_context);

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