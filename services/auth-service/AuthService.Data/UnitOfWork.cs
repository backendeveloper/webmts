using AuthService.Data.Repositories;
using AuthService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore.Storage;

namespace AuthService.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AuthDbContext _context;
    private IDbContextTransaction _transaction;
    private bool _disposed = false;

    private IUserRepository _userRepository;
    private IRefreshTokenRepository _refreshTokenRepository;
    private IRoleRepository _roleRepository;
    private IUserRoleRepository _userRoleRepository;

    public UnitOfWork(AuthDbContext context)
    {
        _context = context;
    }

    public IUserRepository Users => _userRepository ??= new UserRepository(_context);

    public IRefreshTokenRepository RefreshTokens => _refreshTokenRepository ??= new RefreshTokenRepository(_context);

    public IRoleRepository Roles => _roleRepository ??= new RoleRepository(_context);

    public IUserRoleRepository UserRoles => _userRoleRepository ??= new UserRoleRepository(_context);

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