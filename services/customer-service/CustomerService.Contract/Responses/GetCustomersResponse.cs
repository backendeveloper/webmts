namespace CustomerService.Contract.Responses;

public class GetCustomersResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
}