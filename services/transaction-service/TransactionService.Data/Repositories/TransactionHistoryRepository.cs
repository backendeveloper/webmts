using TransactionService.Data.Entities;
using TransactionService.Data.Repositories.Imp;

namespace TransactionService.Data.Repositories;

public class TransactionHistoryRepository : Repository<TransactionHistory>, ITransactionHistoryRepository
{
    public TransactionHistoryRepository(TransactionDbContext context) : base(context)
    {
    }
}