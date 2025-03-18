using TransactionService.Common.Logging;

namespace TransactionService.Client.Middlewares;

public class TraceMiddleware
{
    private readonly RequestDelegate _next;

    public TraceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TraceContext traceContext)
    {
        var traceId = Guid.NewGuid().ToString();
        
        context.Items["TraceId"] = traceId;
        context.Response.Headers.Append("X-Trace-Id", traceId);
        traceContext.TraceId = traceId;

        await _next(context);
    }
}