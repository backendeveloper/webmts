using System.Text.Json;
using AuthService.Client;
using AuthService.Client.Infrastructure.Consul;
using AuthService.Client.Infrastructure.HealthChecks;
using AuthService.Client.Infrastructure.Vault;
using AuthService.Client.Middlewares;
using AuthService.Common.Caching;
using AuthService.Common.Logging;
using AuthService.Data;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Consul;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

var builder = WebApplication.CreateBuilder(args);

var initialConfig = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// builder.Configuration.Clear(); // TODO: burasi hata veriyor
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

try
{
    await builder.Configuration.AddVaultConfiguration(initialConfig);
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading configuration from Vault: {ex.Message}");
    // Hata durumunda devam et, varsayılan yapılandırma kullanılacak
}

AuthService.Client.Infrastructure.Logging.SerilogHelper.ConfigureLogging(builder.Configuration);
builder.Host.UseSerilog();

builder.Services.AddSingleton<IConsulClient>(provider => new ConsulClient(config =>
{
    var consulHost = builder.Configuration["Consul:Host"] ?? "consul";
    var consulPort = int.Parse(builder.Configuration["Consul:Port"] ?? "8500");
    config.Address = new Uri($"http://{consulHost}:{consulPort}");
}));
builder.Services.AddSingleton<IServiceRegistration, ConsulServiceRegistration>();

builder.Services.AddSingleton<IVaultClient>(provider => {
    var vaultUrl = builder.Configuration["Vault:Url"] ?? "http://vault:8200";
    var vaultToken = builder.Configuration["Vault:Token"] ?? "webmts-root-token";
    
    var tokenAuthMethod = new TokenAuthMethodInfo(vaultToken);
    var vaultClientSettings = new VaultClientSettings(vaultUrl, tokenAuthMethod);
    
    return new VaultClient(vaultClientSettings);
});
builder.Services.AddSingleton<ISecretManager, VaultSecretManager>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddCachingServices(builder.Configuration);
builder.Services.AddSingleton<TraceContext>();

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly("AuthService.Data")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
builder.Host.ConfigureContainer<ContainerBuilder>(containerBuilder =>
{
    containerBuilder.RegisterModule<ClientModule>();
});

builder.Services.AddWebMtsHealthChecks(builder.Configuration);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080);
});

var app = builder.Build();

await InitializeVaultAsync(app);

app.Services.GetRequiredService<IServiceRegistration>().RegisterService(
    new ConsulRegistrationInfo
    {
        ServiceId = "auth-service-" + Guid.NewGuid().ToString(),
        ServiceName = "auth-service",
        ServiceAddress = app.Configuration["ServiceSettings:ServiceAddress"] ?? "auth-service",
        ServicePort = 8080,
        HealthCheckEndpoint = "api/auth/health"
    });


if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => { c.RouteTemplate = "api/auth/swagger/{documentName}/swagger.json"; });
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "api/auth/swagger";
        c.SwaggerEndpoint("/api/auth/swagger/v1/swagger.json", "Auth Service API V1");
    });
}

app.UseMiddleware<ApiTraceMiddleware>();
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

async Task InitializeVaultAsync(WebApplication app)
{
    try
    {
        var secretManager = app.Services.GetRequiredService<ISecretManager>();
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        
        var jwtKey = builder.Configuration["Jwt:Key"];
        if (!string.IsNullOrEmpty(jwtKey))
        {
            await secretManager.SetSecretAsync("webmts/auth/jwt", new Dictionary<string, object?>
            {
                { "key", jwtKey }
            });
            logger.LogInformation("JWT key stored in Vault");
        }
        
        await secretManager.SetSecretAsync("webmts/auth/db", new Dictionary<string, object?>
        {
            { "connectionString", builder.Configuration.GetConnectionString("DefaultConnection") }
        });
        
        logger.LogInformation("Vault initialized with secrets");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error initializing Vault");
    }
}