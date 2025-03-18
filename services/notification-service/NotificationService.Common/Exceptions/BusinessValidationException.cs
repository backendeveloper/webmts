namespace NotificationService.Common.Exceptions;

public class BusinessValidationException : Exception
{
    public IEnumerable<string> ValidationErrors { get; }

    public BusinessValidationException(IEnumerable<string> validationErrors)
        : base("One or more business validation errors occurred.")
    {
        ValidationErrors = validationErrors;
    }
    
    public BusinessValidationException(string validationError)
        : this(new[] { validationError })
    {
    }
}