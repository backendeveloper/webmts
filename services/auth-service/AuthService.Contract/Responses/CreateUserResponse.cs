using AuthService.Contract.Dtos;

namespace AuthService.Contract.Responses;

public class CreateUserResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public UserDto User { get; set; }
}