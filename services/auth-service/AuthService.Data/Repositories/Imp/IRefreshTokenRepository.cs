using AuthService.Data.Entities;

namespace AuthService.Data.Repositories.Imp;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task<bool> IsTokenValidAsync(string token, Guid userId);
}