using System.Diagnostics;
using TransactionService.Common.Logging;

namespace TransactionService.Client.Middlewares;

public class ApiTraceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiTraceMiddleware> _logger;

    public ApiTraceMiddleware(RequestDelegate next, ILogger<ApiTraceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, TraceContext traceContext)
    {
        var traceId = Guid.NewGuid().ToString();
        var requestPath = context.Request.Path;
        var requestMethod = context.Request.Method;
        var stopwatch = Stopwatch.StartNew();

        // Set trace ID
        context.Items["TraceId"] = traceId;
        context.Response.Headers.Append("X-Trace-Id", traceId);
        traceContext.TraceId = traceId;

        // Add correlation ID if present in the request headers
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Log the request
        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = requestPath,
            ["RequestMethod"] = requestMethod
        });

        // Capture request body if needed
        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0 && ShouldCaptureRequestBody(context))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Add structured logging info but sanitize sensitive data
            _logger.LogInformation("Request {Method} {Path} started. Body: {RequestBody}",
                requestMethod, requestPath, SanitizeJson(requestBody));
        }
        else
        {
            _logger.LogInformation("Request {Method} {Path} started",
                requestMethod, requestPath);
        }

        // Save original response body stream
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            // Call the next middleware
            await _next(context);

            stopwatch.Stop();

            // Read response body
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            // Copy back to original stream
            await responseBodyStream.CopyToAsync(originalBodyStream);

            // Log response with structured logging
            _logger.LogInformation(
                "Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                requestMethod, requestPath, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);

            // Only log response body for non-success statuses or if it's useful for debugging
            if (context.Response.StatusCode >= 400 && ShouldCaptureResponseBody(context))
            {
                _logger.LogDebug(
                    "Response for {Method} {Path}: Body: {ResponseBody}",
                    requestMethod, requestPath, SanitizeJson(responseBody));
            }
        }
        catch (Exception)
        {
            // Exception is handled by the GlobalExceptionMiddleware, so just restore the original body
            context.Response.Body = originalBodyStream;
            throw; // Rethrow to be handled by GlobalExceptionMiddleware
        }
    }

    private bool ShouldCaptureRequestBody(HttpContext context)
    {
        // Content type based decision
        var contentType = context.Request.ContentType?.ToLower() ?? string.Empty;
        return contentType.Contains("application/json") || contentType.Contains("application/xml");
    }

    private bool ShouldCaptureResponseBody(HttpContext context)
    {
        // Content type based decision
        var contentType = context.Response.ContentType?.ToLower() ?? string.Empty;
        return contentType.Contains("application/json") || contentType.Contains("application/xml");
    }

    private string SanitizeJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        try
        {
            // Sanitize sensitive data using regex patterns
            var sensitiveFields = new[]
            {
                "password", "Password",
                "token", "Token",
                "accessToken", "refreshToken",
                "secret", "apiKey",
                "creditCard", "cardNumber",
                "ssn", "socialSecurity"
            };

            var sanitized = json;
            foreach (var field in sensitiveFields)
            {
                // Simple pattern matching for JSON field values
                var pattern = $"\"{field}\"\\s*:\\s*\"[^\"]+\"";
                var replacement = $"\"{field}\":\"[REDACTED]\"";
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized, pattern, replacement,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }

            return sanitized;
        }
        catch
        {
            return "[Unable to sanitize JSON]";
        }
    }
}