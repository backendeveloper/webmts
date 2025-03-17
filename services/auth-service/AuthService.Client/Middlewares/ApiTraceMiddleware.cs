using System.Diagnostics;
using System.Text.Json;
using AuthService.Common.Logging;

namespace AuthService.Client.Middlewares;

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
        var requestTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        // Trace ID'yi ayarla
        context.Items["TraceId"] = traceId;
        context.Response.Headers.Append("X-Trace-Id", traceId);
        traceContext.TraceId = traceId;

        // İstek gövdesini oku (gerekiyorsa)
        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0 && ShouldCaptureRequestBody(context))
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Orijinal response body stream'ini kaydet
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;
        
        try
        {
            // Bir sonraki middleware'i çağır
            await _next(context);
            
            stopwatch.Stop();
            
            // Response body'yi oku
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            
            // Orijinal stream'e response'u yaz
            await responseBodyStream.CopyToAsync(originalBodyStream);
            
            // Detayları logla (hata olmayan durumlar için)
            LogApiTrace(traceId, requestPath, requestMethod, requestTime, stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode, requestBody, 
                ShouldCaptureResponseBody(context) ? responseBody : string.Empty);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Hatayı logla
            _logger.LogError(ex, "Request {Method} {Path} with TraceId {TraceId} failed after {ElapsedMs}ms",
                requestMethod, requestPath, traceId, stopwatch.ElapsedMilliseconds);
            
            // Orijinal response body stream'ini geri yükle
            context.Response.Body = originalBodyStream;
            
            // Hata fırlat
            throw;
        }
        finally
        {
            // Eğer stream değiştirilirse, orijinal stream'e geri döndür
            if (context.Response.Body != originalBodyStream)
            {
                context.Response.Body = originalBodyStream;
            }
        }
    }

    private void LogApiTrace(string traceId, string path, string method, DateTime requestTime, long elapsedMs,
        int statusCode, string requestBody, string responseBody)
    {
        // Loglama için JSON obje oluştur
        var apiTrace = new
        {
            TraceId = traceId,
            Path = path,
            Method = method,
            RequestTime = requestTime,
            ElapsedMilliseconds = elapsedMs,
            StatusCode = statusCode,
            RequestBody = SanitizeJson(requestBody),
            ResponseBody = SanitizeJson(responseBody)
        };

        _logger.LogInformation("API Trace: {ApiTrace}", JsonSerializer.Serialize(apiTrace));
    }

    private bool ShouldCaptureRequestBody(HttpContext context)
    {
        // Content type'a göre request body'nin yakalanıp yakalanmayacağına karar ver
        var contentType = context.Request.ContentType?.ToLower() ?? string.Empty;
        return contentType.Contains("application/json") || contentType.Contains("application/xml");
    }

    private bool ShouldCaptureResponseBody(HttpContext context)
    {
        // Content type'a göre response body'nin yakalanıp yakalanmayacağına karar ver
        var contentType = context.Response.ContentType?.ToLower() ?? string.Empty;
        return contentType.Contains("application/json") || contentType.Contains("application/xml");
    }

    private string SanitizeJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;

        try
        {
            // Hassas verileri (şifre, token vb.) gizle
            var sanitized = json.Replace("\"password\":", "\"password\":\"[REDACTED]\"")
                              .Replace("\"Password\":", "\"Password\":\"[REDACTED]\"")
                              .Replace("\"accessToken\":", "\"accessToken\":\"[REDACTED]\"")
                              .Replace("\"refreshToken\":", "\"refreshToken\":\"[REDACTED]\"");
            
            return sanitized;
        }
        catch
        {
            return json;
        }
    }
}