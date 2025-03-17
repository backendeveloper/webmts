using System.Text.Json;
using AuthService.Client;
using AuthService.Client.Middlewares;
using AuthService.Common.Caching;
using AuthService.Common.Logging;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

// builder.Configuration.AddConsulConfiguration();
// builder.Services.AddConsulServices(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddSingleton<TraceContext>();

// Log.Logger = new LoggerConfiguration()
//     .ReadFrom.Configuration(builder.Configuration)
//     .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Diagnostics.DeveloperExceptionPageMiddleware"))
//     .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Server.Kestrel"))
//     .Enrich.WithProperty("Application", "Kazan.API")
//     .CreateLogger();
// builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<ClientModule>();
});

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy())
    .AddNpgSql(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "authdb-check",
        tags: ["authdb"]
    )
    .AddRedis(
        builder.Configuration.GetSection("CacheSettings:DistributedCache:ConnectionString").Value ?? string.Empty,
        name: "redis-check",
        tags: new[] { "redis" }
    );


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => { c.RouteTemplate = "api/auth/swagger/{documentName}/swagger.json"; });

    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "api/auth/swagger";
        c.SwaggerEndpoint("/api/auth/swagger/v1/swagger.json", "Auth Service API V1");
    });
}

// app.UseHttpsRedirection();
app.UseMiddleware<TraceMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/api/auth/health", new HealthCheckOptions
{
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    },
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var json = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                error = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(json);
    }
});

app.MapGet("/api/auth", () => "Auth Service is running!");

app.MapControllers();

app.Run();