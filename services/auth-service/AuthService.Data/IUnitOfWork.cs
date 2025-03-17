using AuthService.Data.Repositories.Imp;

namespace AuthService.Data;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    IRoleRepository Roles { get; }
    
    Task<int> CompleteAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}