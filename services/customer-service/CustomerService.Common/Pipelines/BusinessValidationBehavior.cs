using CustomerService.Common.Exceptions;
using MediatR;

namespace CustomerService.Common.Pipelines;

public class BusinessValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IBusinessRule<TRequest>> _businessRules;

    public BusinessValidationBehavior(IEnumerable<IBusinessRule<TRequest>> businessRules)
    {
        _businessRules = businessRules;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        foreach (var rule in _businessRules)
        {
            var result = await rule.ValidateAsync(request);
            if (!result.IsValid) 
                failures.Add(result.ErrorMessage);
        }

        if (failures.Count != 0)
            throw new BusinessValidationException(failures);

        return await next();
    }
}