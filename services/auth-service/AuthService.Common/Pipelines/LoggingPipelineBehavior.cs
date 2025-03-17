using System.Diagnostics;
using AuthService.Common.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Pipelines;

public class LoggingPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingPipelineBehavior<TRequest, TResponse>> _logger;
    private readonly TraceContext _traceContext;

    public LoggingPipelineBehavior(
        ILogger<LoggingPipelineBehavior<TRequest, TResponse>> logger,
        TraceContext traceContext)
    {
        _logger = logger;
        _traceContext = traceContext;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var traceId = _traceContext.TraceId;
        var className = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var parameters = request.GetType()
            .GetProperties()
            .ToDictionary(prop => prop.Name, prop => prop.GetValue(request, null));
        
        _logger.LogInformation("Entering {ClassName} with parameters {@Parameters} for traceId {TraceId} in {MethodType} with {LogDataType}", 
            className, 
            parameters, 
            traceId,
            "MethodEntry",
            "Info");

        TResponse response;
        try
        {
            response = await next();

            stopwatch.Stop();
            
            _logger.LogInformation("Exiting {ClassName} with response {@Response} for traceId {TraceId} Execution Time: {ExecutionTime}ms in {MethodType} with {LogDataType}", 
                className, 
                response, 
                traceId,
                stopwatch.ElapsedMilliseconds,
                "MethodExit",
                "Info");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Error in {MethodName} for traceId {traceId} after {ExecutionTime}ms. Exception: {Message}",
                className,
                traceId,
                stopwatch.ElapsedMilliseconds,
                ex.Message);
            throw;
        }

        return response;
    }
}