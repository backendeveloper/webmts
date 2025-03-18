using AuthService.Data.Entities;
using AuthService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data.Repositories;

public class UserRoleRepository : Repository<UserRole>, IUserRoleRepository
{
    public UserRoleRepository(AuthDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserRole>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserRole>> GetByRoleIdAsync(int roleId)
    {
        return await _context.UserRoles
            .Include(ur => ur.User)
            .Where(ur => ur.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<bool> HasRoleAsync(Guid userId, string roleName)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName);
    }

    public async Task<bool> HasRoleAsync(Guid userId, int roleId)
    {
        return await _context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);
    }
}