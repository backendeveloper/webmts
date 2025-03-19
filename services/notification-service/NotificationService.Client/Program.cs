using System.Text.Json;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Consul;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using NotificationService.Client;
using NotificationService.Client.Infrastructure.Consul;
using NotificationService.Client.Infrastructure.HealthChecks;
using NotificationService.Client.Infrastructure.Logging;
using NotificationService.Client.Infrastructure.Vault;
using NotificationService.Client.Middlewares;
using NotificationService.Common.Configuration;
using NotificationService.Common.Logging;
using NotificationService.Common.ServiceDiscovery;
using NotificationService.Common.ServiceDiscovery.Consul;
using NotificationService.Common.ServiceDiscovery.Interfaces;
using NotificationService.Data;
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

SerilogHelper.ConfigureLogging(initialConfig);
builder.Host.UseSerilog();

builder.Services.AddSingleton<IConsulClient>(_ =>
{
    var consulHost = initialConfig["Consul:Host"] ?? "consul";
    var consulPort = int.Parse(initialConfig["Consul:Port"] ?? "8500");
    return new ConsulClient(config => { config.Address = new Uri($"http://{consulHost}:{consulPort}"); });
});

builder.Services.Configure<ServiceConfig>(options =>
{
    options.Name = initialConfig["Service:Name"] ?? "notification-service";
    options.Address = initialConfig["Service:Address"] ?? "notification-service";
    options.Port = int.Parse(initialConfig["Service:Port"] ?? "8080");
    options.HealthCheckEndpoint = initialConfig["Service:HealthCheckEndpoint"] ?? "api/notification/health";
    options.Tags = (initialConfig.GetSection("Service:Tags").Get<string[]>() ?? ["notification", "api"]);
});

builder.Services.AddSingleton<IKeyValueStore, ConsulKeyValueStore>();
builder.Services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
builder.Services.AddHostedService<ConsulHostedService>();

try
{
    var consulClient = builder.Services.BuildServiceProvider().GetRequiredService<IConsulClient>();
    var loggerFactory = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("ConsulConfiguration");
    var serviceName = initialConfig["Service:Name"] ?? "notification-service";
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

builder.Services.AddConsulConfigurationMonitoring();

try
{
    await builder.Configuration.AddVaultConfiguration(initialConfig);
}
catch (Exception ex)
{
    Console.WriteLine($"Error loading configuration from Vault: {ex.Message}");
}

builder.Services.AddSingleton<IVaultClient>(_ =>
{
    var vaultUrl = builder.Configuration["Vault:Url"] ?? "http://vault:8200";
    var vaultToken = builder.Configuration["Vault:Token"] ?? "webmts-root-token";

    var tokenAuthMethod = new TokenAuthMethodInfo(vaultToken);
    var vaultClientSettings = new VaultClientSettings(vaultUrl, tokenAuthMethod);

    return new VaultClient(vaultClientSettings);
});
builder.Services.AddSingleton<ISecretManager, VaultSecretManager>();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<TraceContext>();

builder.Services.AddHostedService<KibanaIndexInitializer>();

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        x => x.MigrationsAssembly("NotificationService.Data")));

builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));

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

var app = builder.Build();

await InitializeVaultAsync(app);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c => { c.RouteTemplate = "api/notification/swagger/{documentName}/swagger.json"; });
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "api/notification/swagger";
        c.SwaggerEndpoint("/api/notification/swagger/v1/swagger.json", "Notification Service API V1");
    });
}

app.UseGlobalExceptionHandler();
app.UseMiddleware<ApiTraceMiddleware>();
app.UseMiddleware<TraceMiddleware>();
app.UseAuthorization();

app.MapHealthChecks("/api/notification/health", new HealthCheckOptions
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

app.MapGet("/api/notification", () => "Notification Service is running!");

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
            await secretManager.SetSecretAsync("webmts/notification/jwt", new Dictionary<string, object?>
            {
                { "key", jwtKey }
            });
            logger.LogInformation("JWT key stored in Vault");
        }

        await secretManager.SetSecretAsync("webmts/notification/db", new Dictionary<string, object?>
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