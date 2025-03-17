using System.Text.Json;
using AuthService.Client;
using AuthService.Client.Infrastructure.Consul;
using AuthService.Client.Infrastructure.HealthChecks;
using AuthService.Client.Infrastructure.Vault;
using AuthService.Client.Middlewares;
using AuthService.Common.Caching;
using AuthService.Common.Configuration;
using AuthService.Common.Logging;
using AuthService.Common.ServiceDiscovery;
using AuthService.Common.ServiceDiscovery.Consul;
using AuthService.Common.ServiceDiscovery.Interfaces;
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

AuthService.Client.Infrastructure.Logging.SerilogHelper.ConfigureLogging(initialConfig);
builder.Host.UseSerilog();

// builder.Configuration.Clear(); // TODO: burasi hata veriyor
// builder.Configuration
//     .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
//     .AddEnvironmentVariables();

builder.Services.AddSingleton<IConsulClient>(_ => {
    var consulHost = initialConfig["Consul:Host"] ?? "consul";
    var consulPort = int.Parse(initialConfig["Consul:Port"] ?? "8500");
    return new ConsulClient(config => {
        config.Address = new Uri($"http://{consulHost}:{consulPort}");
    });
});

builder.Services.Configure<ServiceConfig>(options => {
    options.Name = initialConfig["Service:Name"] ?? "auth-service";
    options.Address = initialConfig["Service:Address"] ?? "auth-service";
    options.Port = int.Parse(initialConfig["Service:Port"] ?? "8080");
    options.HealthCheckEndpoint = initialConfig["Service:HealthCheckEndpoint"] ?? "api/auth/health";
    options.Tags = (initialConfig.GetSection("Service:Tags").Get<string[]>() ?? new[] { "auth", "api" });
});

builder.Services.AddSingleton<IKeyValueStore, ConsulKeyValueStore>();
builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
builder.Services.AddHostedService<ConsulHostedService>();

try 
{
    var consulClient = builder.Services.BuildServiceProvider().GetRequiredService<IConsulClient>();
    var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("ConsulConfiguration");
    var serviceName = initialConfig["Service:Name"] ?? "auth-service";
    var environment = builder.Environment.EnvironmentName;
    
    await initialConfig.SyncAppSettingsToConsulAsync(consulClient, serviceName, environment, logger);
    
    builder.Configuration.AddConsulJsonConfiguration(
        consulClient, 
        serviceName, 
        environment, 
        logger, 
        builder.Services.BuildServiceProvider());
    
    Console.WriteLine("Consul'dan JSON yapılandırması başarıyla eklendi");
}
catch (Exception ex) 
{
    Console.WriteLine($"Consul'dan yapılandırma eklenirken hata: {ex.Message}");
}

try
{
    await builder.Configuration.AddVaultConfiguration(initialConfig);
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading configuration from Vault: {ex.Message}");
}

builder.Services.AddSingleton<IVaultClient>(provider =>
{
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

builder.WebHost.ConfigureKestrel(options => { options.ListenAnyIP(8080); });

// try
// {
//     var consulClient = builder.Services.BuildServiceProvider().GetRequiredService<IConsulClient>();
//     var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
//     var logger = loggerFactory.CreateLogger("ConsulConfiguration");
//     var serviceName = initialConfig["Service:Name"] ?? "auth-service";
//     var environment = builder.Environment.EnvironmentName;
//
//     // JSON dosyalarını Consul'a senkronize et
//     await initialConfig.SyncAppSettingsToConsulAsync(consulClient, serviceName, environment, logger);
//
//     // Consul'dan JSON yapılandırmasını yükle
//     builder.Configuration.AddConsulJsonConfiguration(consulClient, serviceName, environment, logger);
//
//     Console.WriteLine("Consul'dan JSON yapılandırması başarıyla eklendi");
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"Consul'dan dinamik yapılandırma eklenirken hata: {ex.Message}");
// }

var app = builder.Build();

await InitializeVaultAsync(app);

// try
// {
//     var serviceName = initialConfig["Service:Name"] ?? "auth-service";
//     var keyValueStore = app.Services.GetService<IKeyValueStore>();
//
//     if (keyValueStore != null)
//     {
//         // appsettings.json'ı Consul'a senkronize et
//         await keyValueStore.SyncAllConfigurationsToConsulAsync(serviceName);
//         app.Logger.LogInformation("appsettings.json yapılandırması Consul'a başarıyla senkronize edildi");
//     }
// }
// catch (Exception ex)
// {
//     app.Logger.LogWarning("Consul yapılandırma senkronizasyonu başarısız: {ErrorMessage}", ex.Message);
// }


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

app.UseConsul();

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