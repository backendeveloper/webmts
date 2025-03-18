using System.Diagnostics;
using System.Text.Json;
using CustomerService.Common.Logging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CustomerService.Common.Pipelines;

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
        var requestType = typeof(TRequest).Name;
        var traceId = _traceContext.TraceId;
        var stopwatch = Stopwatch.StartNew();

        // Create request ID for correlation
        var requestId = Guid.NewGuid().ToString();

        // Sanitize request parameters
        var parameters = SanitizeParameters(request);

        // Use structured logging with scopes
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RequestId"] = requestId,
            ["RequestType"] = requestType,
            ["TraceId"] = traceId,
        });

        _logger.LogInformation(
            "Handling request {RequestType} {RequestId} with parameters {@Parameters} TraceId: {TraceId}",
            requestType, requestId, parameters, traceId);

        try
        {
            // Execute the request handler
            var response = await next();

            stopwatch.Stop();

            // For success responses, log with sanitized response data
            _logger.LogInformation(
                "Request {RequestType} {RequestId} handled successfully in {ElapsedMilliseconds}ms TraceId: {TraceId}",
                requestType, requestId, stopwatch.ElapsedMilliseconds, traceId);

            // Log detailed response only at debug level
            _logger.LogDebug(
                "Response for {RequestType} {RequestId}: {@Response}",
                requestType, requestId, SanitizeResponse(response));

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Exception will be caught by our GlobalExceptionMiddleware, so just log and rethrow
            _logger.LogError(ex,
                "Error handling request {RequestType} {RequestId} after {ElapsedMilliseconds}ms TraceId: {TraceId}",
                requestType, requestId, stopwatch.ElapsedMilliseconds, traceId);

            throw;
        }
    }

    private object SanitizeParameters(TRequest request)
    {
        try
        {
            // Convert to dictionary
            var dict = request.GetType()
                .GetProperties()
                .ToDictionary(
                    prop => prop.Name,
                    prop => SanitizeValue(prop.Name, prop.GetValue(request, null)));

            return dict;
        }
        catch
        {
            // If we can't convert to dictionary, use a safe fallback
            return new { Type = typeof(TRequest).Name };
        }
    }

    private object SanitizeValue(string propertyName, object? value)
    {
        var sensitiveProperties = new[]
        {
            "password", "token", "accessToken", "refreshToken", "secret", "apiKey",
            "creditCard", "cardNumber", "ssn", "socialSecurity"
        };

        if (value == null)
            return null!;

        if (sensitiveProperties.Any(p =>
                propertyName.Contains(p, StringComparison.OrdinalIgnoreCase)))
            return "[REDACTED]";

        return value;
    }

    private object? SanitizeResponse(TResponse response)
    {
        try
        {
            if (response == null)
                return null;

            // If it's a simple type, return as is
            if (response.GetType().IsPrimitive || response is string)
                return response;

            // Try to sanitize as JSON
            var json = JsonSerializer.Serialize(response);

            // Use the same sanitization logic for passwords, tokens, etc.
            var sensitiveFields = new[]
            {
                "password", "token", "accessToken", "refreshToken",
                "secret", "apiKey", "creditCard", "cardNumber"
            };

            var sanitized = json;
            foreach (var field in sensitiveFields)
            {
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]+\"";
                var replacement = $"\"{field}\":\"[REDACTED]\"";
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized, pattern, replacement,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            // Parse back to object to return in structured form
            return JsonSerializer.Deserialize<object>(sanitized);
        }
        catch
        {
            // If we can't safely sanitize, return type info only
            return new { Type = typeof(TResponse).Name };
        }
    }
}