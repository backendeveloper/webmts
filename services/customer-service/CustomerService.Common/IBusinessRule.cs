namespace CustomerService.Common;

public interface IBusinessRule<TRequest>
{
    Task<(bool IsValid, string ErrorMessage)> ValidateAsync(TRequest request);
}