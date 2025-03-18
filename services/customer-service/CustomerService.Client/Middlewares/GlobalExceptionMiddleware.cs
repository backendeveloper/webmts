using System.Net;
using System.Text.Json;
using CustomerService.Common.Exceptions;
using CustomerService.Common.Logging;

namespace CustomerService.Client.Middlewares;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context, TraceContext traceContext)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex, traceContext.TraceId);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, string traceId)
    {
        var statusCode = GetStatusCode(exception);
        var response = new
        {
            status = statusCode,
            traceId = traceId,
            title = GetTitle(exception),
            detail = _environment.IsDevelopment() ? exception.ToString() : exception.Message
        };

        // Log with structured data
        _logger.LogError(exception,
            "Error processing request {TraceId} {Path}: {ErrorType} - {ErrorMessage}",
            traceId,
            context.Request.Path,
            exception.GetType().Name,
            exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            BusinessValidationException => (int)HttpStatusCode.BadRequest,
            KeyNotFoundException => (int)HttpStatusCode.NotFound,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            // Add other specific exceptions here
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            BusinessValidationException => "Validation Error",
            // Add other specific exception titles
            _ => "Server Error"
        };
    }
}

// Extension method for clean Program.cs usage
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalExceptionMiddleware>();
    }
}