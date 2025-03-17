using AuthService.Common.ServiceDiscovery.Consul;
using AuthService.Common.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Common.ServiceDiscovery;

public class ConsulHostedService : IHostedService
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IKeyValueStore _keyValueStore;
    private readonly ILogger<ConsulHostedService> _logger;
    private readonly ServiceConfig _serviceConfig;
    private string _serviceId;

    public ConsulHostedService(
        IServiceDiscovery serviceDiscovery,
        IKeyValueStore keyValueStore,
        IOptions<ServiceConfig> serviceConfig,
        ILogger<ConsulHostedService> logger)
    {
        _serviceDiscovery = serviceDiscovery;
        _keyValueStore = keyValueStore;
        _logger = logger;
        _serviceConfig = serviceConfig.Value;
        _serviceId = $"{_serviceConfig.Name}-{Guid.NewGuid()}";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Consul integration for service {ServiceName}", _serviceConfig.Name);
        try
        {
            await _serviceDiscovery.RegisterServiceAsync(_serviceConfig.Name, _serviceId);
            await InitializeConfigurationAsync();
            _logger.LogInformation("Service {ServiceName} with ID {ServiceId} registered in Consul", 
                _serviceConfig.Name, _serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service {ServiceName} with Consul", _serviceConfig.Name);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Consul integration for service {ServiceName}", _serviceConfig.Name);
        try
        {
            await _serviceDiscovery.DeregisterServiceAsync(_serviceId);
            _logger.LogInformation("Service {ServiceName} with ID {ServiceId} deregistered from Consul", 
                _serviceConfig.Name, _serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deregister service {ServiceName} from Consul", _serviceConfig.Name);
        }
    }

    private async Task InitializeConfigurationAsync()
    {
        try
        {
            // Servis zaten yapılandırıldı mı kontrol et
            var initialized = await _keyValueStore.GetValueAsync($"{_serviceConfig.Name}/config/initialized");
            if (initialized == "true")
            {
                _logger.LogInformation("Configuration for {ServiceName} already exists in Consul", _serviceConfig.Name);
                return;
            }

            // Temel yapılandırmaları ekle
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/jwt/issuer", "WebmtsAuthService");
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/jwt/audience", "WebmtsClient");
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/jwt/expiryMinutes", "60");
            
            // Rate limiting yapılandırmaları
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/ratelimit/enabled", "true");
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/ratelimit/period", "60");
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/ratelimit/limit", "100");
            
            // İşaretleme yap
            await _keyValueStore.SetValueAsync($"{_serviceConfig.Name}/config/initialized", "true");
            
            _logger.LogInformation("Initial configuration for {ServiceName} stored in Consul", _serviceConfig.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize configuration in Consul for {ServiceName}", _serviceConfig.Name);
        }
    }
}