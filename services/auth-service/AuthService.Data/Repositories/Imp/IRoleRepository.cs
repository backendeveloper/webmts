using AuthService.Data.Entities;

namespace AuthService.Data.Repositories.Imp;

public interface IRoleRepository : IRepository<Role>
{
    Task<Role> GetByNameAsync(string name);
}