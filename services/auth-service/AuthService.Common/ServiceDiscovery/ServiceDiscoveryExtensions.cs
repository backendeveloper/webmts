// using AuthService.Common.Configuration;
// using AuthService.Common.ServiceDiscovery.Consul;
// using AuthService.Common.ServiceDiscovery.Interfaces;
// using Consul;
// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Options;
//
// namespace AuthService.Common.ServiceDiscovery;
//
// public static class ServiceDiscoveryExtensions
// {
//     public static IServiceCollection AddConsulServices(this IServiceCollection services, IConfiguration configuration)
//     {
//         services.Configure<ConsulConfig>(configuration.GetSection("Consul"));
//         services.Configure<ServiceConfig>(configuration.GetSection("Service"));
//
//         services.AddSingleton<IConsulClient>(sp =>
//         {
//             var consulConfig = sp.GetRequiredService<IOptions<ConsulConfig>>().Value;
//             return new ConsulClient(config =>
//             {
//                 config.Address = new Uri($"{consulConfig.Scheme}://{consulConfig.Host}:{consulConfig.Port}");
//             });
//         });
//
//         services.AddSingleton<IKeyValueStore>(sp => new ConsulKeyValueStore(
//             sp.GetRequiredService<IConsulClient>(),
//             sp.GetRequiredService<ILogger<ConsulKeyValueStore>>(),
//             configuration));
//
//         services.AddSingleton<IServiceDiscovery, ConsulServiceDiscovery>();
//
//         services.AddHostedService<ConfigurationSynchronizer>();
//         
//         services.AddHostedService<ConsulConfigurationHostedService>();
//
//         return services;
//     }
//
//     public static IConfigurationBuilder AddConsulConfiguration(
//         this IConfigurationBuilder builder,
//         IKeyValueStore keyValueStore,
//         string serviceName,
//         ILogger logger)
//     {
//         return builder.Add(new DynamicConsulConfigurationSource(
//             keyValueStore,
//             serviceName,
//             logger,
//             TimeSpan.FromMinutes(1)));
//     }
//     
//     public class ConsulConfigurationHostedService : IHostedService
// {
//     private readonly IConfiguration _configuration;
//     private readonly IKeyValueStore _keyValueStore;
//     private readonly ILogger<ConsulConfigurationHostedService> _logger;
//     
//     public ConsulConfigurationHostedService(
//         IConfiguration configuration,
//         IKeyValueStore keyValueStore,
//         ILogger<ConsulConfigurationHostedService> logger)
//     {
//         _configuration = configuration;
//         _keyValueStore = keyValueStore;
//         _logger = logger;
//     }
//     
//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         var serviceName = _configuration["Service:Name"] ?? "auth-service";
//         
//         Task.Run(async () => {
//             try {
//                 var isInitialized = await _keyValueStore.GetValueAsync($"{serviceName}/config/initialized");
//                 
//                 if (isInitialized != "true") {
//                     _logger.LogInformation("Initializing configuration in Consul for service {ServiceName}", serviceName);
//                     await InitializeConfigurationAsync(serviceName);
//                 } else {
//                     _logger.LogInformation("Configuration already exists in Consul for service {ServiceName}", serviceName);
//                 }
//             } catch (Exception ex) {
//                 _logger.LogError(ex, "Error checking/initializing Consul configuration");
//             }
//         }, cancellationToken);
//         
//         return Task.CompletedTask;
//     }
//     
//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         return Task.CompletedTask;
//     }
//     
//     private async Task InitializeConfigurationAsync(string serviceName)
//     {
//         var sections = new[] { "Jwt", "TokenSettings", "Service", "RateLimit", "FeatureFlags" };
//         
//         foreach (var section in sections) {
//             var configSection = _configuration.GetSection(section);
//             if (configSection.Exists() && configSection.GetChildren().Any()) {
//                 await SyncSectionToConsulAsync(configSection, $"{serviceName}/config/{section.ToLowerInvariant()}");
//             }
//         }
//         
//         await _keyValueStore.SetValueAsync($"{serviceName}/config/initialized", "true");
//         await _keyValueStore.SetValueAsync($"{serviceName}/config/last_sync", DateTime.UtcNow.ToString("o"));
//     }
//     
//     private async Task SyncSectionToConsulAsync(IConfigurationSection section, string consulPrefix)
//     {
//         foreach (var child in section.GetChildren()) {
//             if (child.Value != null) {
//                 var consulKey = $"{consulPrefix}/{child.Key}";
//                 await _keyValueStore.SetValueAsync(consulKey, child.Value);
//                 _logger.LogDebug("Set configuration in Consul: {Key} = {Value}", consulKey, child.Value);
//             } else
//                 await SyncSectionToConsulAsync(child, $"{consulPrefix}/{child.Key}");
//         }
//     }
// }
// }