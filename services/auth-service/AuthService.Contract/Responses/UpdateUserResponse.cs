using AuthService.Contract.Dtos;

namespace AuthService.Contract.Responses;

public class UpdateUserResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public UserDto User { get; set; }
}