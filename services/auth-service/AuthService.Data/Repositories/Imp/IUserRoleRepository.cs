using AuthService.Data.Entities;

namespace AuthService.Data.Repositories.Imp;

public interface IUserRoleRepository : IRepository<UserRole>
{
    Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<UserRole>> GetByRoleIdAsync(int roleId);
    Task<bool> HasRoleAsync(Guid userId, string roleName);
    Task<bool> HasRoleAsync(Guid userId, int roleId);
}