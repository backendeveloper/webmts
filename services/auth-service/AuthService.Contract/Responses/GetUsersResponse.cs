using AuthService.Contract.Dtos;

namespace AuthService.Contract.Responses;

public class GetUsersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public List<UserDto> Users { get; set; } = new List<UserDto>();
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}