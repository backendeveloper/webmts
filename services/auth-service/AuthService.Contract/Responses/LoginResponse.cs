using AuthService.Contract.Dtos;

namespace AuthService.Contract.Responses;

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpirationTime { get; set; }
    public UserDto User { get; set; }
}