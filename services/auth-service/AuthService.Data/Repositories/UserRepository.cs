using AuthService.Data.Entities;
using AuthService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AuthDbContext context) : base(context)
    {
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User> GetWithRolesAsync(Guid userId)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}