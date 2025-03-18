using CustomerService.Data.Entities;

namespace CustomerService.Data.Repositories.Imp;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer> GetByUsernameAsync(string username);
    Task<Customer> GetByEmailAsync(string email);
    Task<IEnumerable<Customer>> GetAllPaginatedAsync(int page, int pageSize, bool includeRoles = false);
    Task<int> CountAsync();
}