using AuthService.Data.Entities;

namespace AuthService.Data.Repositories.Imp;

public interface IUserRepository : IRepository<User>
{
    Task<User> GetByUsernameAsync(string username);
    Task<User> GetByEmailAsync(string email);
    Task<User> GetWithRolesAsync(Guid userId);
}