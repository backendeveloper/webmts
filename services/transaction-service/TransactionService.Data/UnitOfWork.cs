using Microsoft.EntityFrameworkCore.Storage;
using TransactionService.Data.Repositories;
using TransactionService.Data.Repositories.Imp;

namespace TransactionService.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly TransactionDbContext _context;
    private IDbContextTransaction _transaction;
    private bool _disposed = false;

    private ITransactionRepository _transactionRepository;
    private ITransactionHistoryRepository _transactionHistoryRepository;

    public UnitOfWork(TransactionDbContext context)
    {
        _context = context;
    }

    public ITransactionRepository Transactions => _transactionRepository ??= new TransactionRepository(_context);

    public ITransactionHistoryRepository TransactionHistories =>
        _transactionHistoryRepository ??= new TransactionHistoryRepository(_context);

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