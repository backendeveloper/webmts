using CustomerService.Data.Repositories.Imp;

namespace CustomerService.Data;

public interface IUnitOfWork : IDisposable
{
    ICustomerRepository Customers { get; }

    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}