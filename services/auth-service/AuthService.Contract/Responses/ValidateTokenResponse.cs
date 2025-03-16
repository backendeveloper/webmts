using AuthService.Contract.Dtos;

namespace AuthService.Contract.Responses;

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public UserDto User { get; set; }
}