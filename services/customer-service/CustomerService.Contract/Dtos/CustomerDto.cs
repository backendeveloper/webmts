namespace CustomerService.Contract.Dtos;

public class CustomerDto
{
    public Guid Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
}