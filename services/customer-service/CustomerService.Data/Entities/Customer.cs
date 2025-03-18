namespace CustomerService.Data.Entities;

public class Customer : BaseEntity
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}