using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace CustomerService.Client.Infrastructure.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddWebMtsHealthChecks(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "api" })
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
                name: "customerdb-check",
                tags: new[] { "database", "postgresql", "customerdb" }
            )
            .AddElasticsearch(
                configuration["Elasticsearch:Url"] ?? "http://elasticsearch:9200",
                name: "elasticsearch-check",
                tags: new[] { "observability", "elasticsearch" }
            );
        // TODO: Burasi hata veriyor
        // .AddConsul(
        //     setup => setup.UseConsulClient(serviceProvider => serviceProvider.GetRequiredService<IConsulClient>()),
        //     name: "consul-check",
        //     tags: new[] { "serviceregistry", "consul" }
        // );

        return services;
    }

    public static WebApplication MapWebMtsHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/api/customer/health", new HealthCheckOptions
        {
            Predicate = _ => true,
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = WriteHealthCheckResponse
        });

        app.MapHealthChecks("/api/customer/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("database") || check.Tags.Contains("cache"),
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = WriteHealthCheckResponse
        });

        app.MapHealthChecks("/api/customer/health/live", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("self") || check.Tags.Contains("api"),
            AllowCachingResponses = false,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            ResponseWriter = WriteHealthCheckResponse
        });

        return app;
    }

    private static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        var response = new
        {
            Status = report.Status.ToString(),
            Duration = report.TotalDuration,
            Timestamp = DateTime.UtcNow,
            Services = report.Entries.Select(e => new
            {
                Service = e.Key,
                Status = e.Value.Status.ToString(),
                Duration = e.Value.Duration,
                Tags = e.Value.Tags,
                Info = e.Value.Data.Count > 0 ? e.Value.Data : null,
                Error = e.Value.Exception?.Message,
                Exception = e.Value.Exception != null
                    ? new
                    {
                        Message = e.Value.Exception.Message,
                        StackTrace = e.Value.Exception.StackTrace
                    }
                    : null
            })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}