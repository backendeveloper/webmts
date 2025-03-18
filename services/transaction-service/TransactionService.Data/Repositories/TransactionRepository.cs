using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TransactionService.Data.Entities;
using TransactionService.Data.Repositories.Imp;

namespace TransactionService.Data.Repositories;

public class TransactionRepository : Repository<Transaction>, ITransactionRepository
{
    public TransactionRepository(TransactionDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Transaction>> GetTransactionsByCustomerIdAsync(Guid customerId)
    {
        return await _context.Transactions
            .Where(t => t.CustomerId == customerId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<Transaction> GetTransactionWithHistoryAsync(Guid id)
    {
        return await _context.Transactions
            .Include(t => t.TransactionHistories)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<Transaction>> GetAsync(Expression<Func<Transaction, bool>> predicate,
        object orderBy)
    {
        return await _context.Set<Transaction>().Where(predicate).ToListAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetAsync(Expression<Func<Transaction, bool>> predicate = null,
        Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy = null,
        string includeString = null,
        bool disableTracking = true)
    {
        IQueryable<Transaction> query = _context.Set<Transaction>();

        if (disableTracking) query = query.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(includeString)) query = query.Include(includeString);

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null)
            return await orderBy(query).ToListAsync();

        return await query.ToListAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetAsync(Expression<Func<Transaction, bool>> predicate = null,
        Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy = null,
        List<Expression<Func<Transaction, object>>> includes = null,
        bool disableTracking = true)
    {
        IQueryable<Transaction> query = _context.Set<Transaction>();

        if (disableTracking) query = query.AsNoTracking();

        if (includes != null) query = includes.Aggregate(query, (current, include) => current.Include(include));

        if (predicate != null) query = query.Where(predicate);

        if (orderBy != null)
            return await orderBy(query).ToListAsync();

        return await query.ToListAsync();
    }

    public Task<object> GetAsync(Func<Transaction, bool> predicate, Func<IQueryable<Transaction>, IOrderedQueryable<Transaction>> orderBy)
    {
        throw new NotImplementedException();
    }
}