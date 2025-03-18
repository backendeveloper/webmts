using System.Linq.Expressions;
using TransactionService.Data.Entities;

namespace TransactionService.Data.Repositories.Imp;

public interface ITransactionRepository : IRepository<Transaction>
{
    Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(Guid customerId);
    Task<Transaction> GetTransactionWithHistoryAsync(Guid id);
    Task<IReadOnlyList<Transaction>> GetAsync(Expression<Func<Transaction, bool>> predicate, object orderBy);
    Task<IReadOnlyList<Transaction>> GetAsync(Expression<Func<Transaction, bool>> predicate = null,
        Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy = null,
        string includeString = null,
        bool disableTracking = true);
    Task<IReadOnlyList<Transaction>> GetAsync(Expression<Func<Transaction, bool>> predicate = null,
        Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy = null,
        List<Expression<Func<Transaction, object>>> includes = null,
        bool disableTracking = true);

    Task<object> GetAsync(Func<Transaction, bool> predicate, Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy);
}