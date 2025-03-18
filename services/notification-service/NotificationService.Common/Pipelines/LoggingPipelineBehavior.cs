using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Common.Logging;

namespace NotificationService.Common.Pipelines;

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

        var requestId = Guid.NewGuid().ToString();

        var parameters = SanitizeParameters(request);

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
            var response = await next();

            stopwatch.Stop();

            _logger.LogInformation(
                "Request {RequestType} {RequestId} handled successfully in {ElapsedMilliseconds}ms TraceId: {TraceId}",
                requestType, requestId, stopwatch.ElapsedMilliseconds, traceId);

            _logger.LogDebug(
                "Response for {RequestType} {RequestId}: {@Response}",
                requestType, requestId, SanitizeResponse(response));

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

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
            var dict = request.GetType()
                .GetProperties()
                .ToDictionary(
                    prop => prop.Name,
                    prop => SanitizeValue(prop.Name, prop.GetValue(request, null)));

            return dict;
        }
        catch
        {
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

            if (response.GetType().IsPrimitive || response is string)
                return response;

            var json = JsonSerializer.Serialize(response);

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

            return JsonSerializer.Deserialize<object>(sanitized);
        }
        catch
        {
            return new { Type = typeof(TResponse).Name };
        }
    }
}