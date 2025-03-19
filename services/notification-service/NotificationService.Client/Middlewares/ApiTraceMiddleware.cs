using System.Diagnostics;
using NotificationService.Common.Logging;

namespace NotificationService.Client.Middlewares;

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

        context.Items["TraceId"] = traceId;
        context.Response.Headers.Append("X-Trace-Id", traceId);
        traceContext.TraceId = traceId;

        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? Guid.NewGuid().ToString();
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        using var loggingScope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["TraceId"] = traceId,
            ["CorrelationId"] = correlationId,
            ["RequestPath"] = requestPath,
            ["RequestMethod"] = requestMethod
        });

        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0 && ShouldCaptureRequestBody(context))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            _logger.LogInformation("Request {Method} {Path} started. Body: {RequestBody}",
                requestMethod, requestPath, SanitizeJson(requestBody));
        }
        else
        {
            _logger.LogInformation("Request {Method} {Path} started",
                requestMethod, requestPath);
        }

        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            stopwatch.Stop();

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);

            await responseBodyStream.CopyToAsync(originalBodyStream);

            _logger.LogInformation(
                "Request {Method} {Path} completed in {ElapsedMilliseconds}ms with status {StatusCode}",
                requestMethod, requestPath, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);

            if (context.Response.StatusCode >= 400 && ShouldCaptureResponseBody(context))
            {
                _logger.LogDebug(
                    "Response for {Method} {Path}: Body: {ResponseBody}",
                    requestMethod, requestPath, SanitizeJson(responseBody));
            }
        }
        catch (Exception)
        {
            context.Response.Body = originalBodyStream;
            throw;
        }
    }

    private bool ShouldCaptureRequestBody(HttpContext context)
    {
        var contentType = context.Request.ContentType?.ToLower() ?? string.Empty;

        return contentType.Contains("application/json") || contentType.Contains("application/xml");
    }

    private bool ShouldCaptureResponseBody(HttpContext context)
    {
        var contentType = context.Response.ContentType?.ToLower() ?? string.Empty;
        return contentType.Contains("application/json") || contentType.Contains("application/xml");
    }

    private string SanitizeJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        try
        {
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