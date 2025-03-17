using AuthService.Data.Entities;
using AuthService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AuthDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> IsTokenValidAsync(string token, Guid userId)
    {
        return await _context.RefreshTokens
            .AnyAsync(rt => rt.Token == token &&
                            rt.UserId == userId &&
                            !rt.IsRevoked &&
                            rt.ExpiresAt > DateTime.UtcNow);
    }
}