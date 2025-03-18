using CustomerService.Data.Entities;
using CustomerService.Data.Repositories.Imp;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Data.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(CustomerDbContext context) : base(context)
    {
    }

    public async Task<Customer> GetByUsernameAsync(string username)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<Customer> GetByEmailAsync(string email)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<Customer>> GetAllPaginatedAsync(int page, int pageSize, bool includeRoles = false)
    {
        var query = _context.Customers.AsQueryable();

        return await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> CountAsync()
    {
        return await _context.Customers.CountAsync();
    }
}