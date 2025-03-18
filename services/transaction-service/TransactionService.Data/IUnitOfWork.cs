using TransactionService.Data.Repositories.Imp;

namespace TransactionService.Data;

public interface IUnitOfWork : IDisposable
{
    ITransactionRepository Transactions { get; }
    ITransactionHistoryRepository TransactionHistories { get; }

    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}