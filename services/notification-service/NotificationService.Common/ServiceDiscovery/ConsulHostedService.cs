using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Common.Configuration;
using NotificationService.Common.ServiceDiscovery.Consul;
using NotificationService.Common.ServiceDiscovery.Interfaces;

namespace NotificationService.Common.ServiceDiscovery;

public class ConsulHostedService : IHostedService
{
    private readonly IServiceDiscovery _serviceDiscovery;
    private readonly IConsulClient _consulClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ConsulHostedService> _logger;
    private readonly ServiceConfig _serviceConfig;
    private readonly string _serviceId;
    private readonly string _environment;

    public ConsulHostedService(
        IServiceDiscovery serviceDiscovery,
        IConsulClient consulClient,
        IConfiguration configuration,
        IOptions<ServiceConfig> serviceConfig,
        ILogger<ConsulHostedService> logger,
        IHostEnvironment environment)
    {
        _serviceDiscovery = serviceDiscovery;
        _consulClient = consulClient;
        _configuration = configuration;
        _logger = logger;
        _serviceConfig = serviceConfig.Value;
        _serviceId = $"{_serviceConfig.Name}-{Guid.NewGuid()}";
        _environment = environment.EnvironmentName;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Consul integration for service {ServiceName}", _serviceConfig.Name);

        try
        {
            await _serviceDiscovery.RegisterServiceAsync(_serviceConfig.Name, _serviceId);

            await _configuration.SyncAppSettingsToConsulAsync(
                _consulClient,
                _serviceConfig.Name,
                _environment,
                _logger);

            _logger.LogInformation(
                "Service {ServiceName} with ID {ServiceId} registered in Consul and configuration synced",
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
}