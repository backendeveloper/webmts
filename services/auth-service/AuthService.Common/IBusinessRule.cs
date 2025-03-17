namespace AuthService.Common;

public interface IBusinessRule<TRequest>
{
    Task<(bool IsValid, string ErrorMessage)> ValidateAsync(TRequest request);
}