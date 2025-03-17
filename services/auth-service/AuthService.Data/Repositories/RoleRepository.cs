using AuthService.Data.Entities;
using AuthService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data.Repositories;

public class RoleRepository : Repository<Role>, IRoleRepository
{
    public RoleRepository(AuthDbContext context) : base(context)
    {
    }

    public async Task<Role> GetByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == name);
    }
}