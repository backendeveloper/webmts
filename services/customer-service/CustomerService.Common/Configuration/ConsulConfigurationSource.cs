using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CustomerService.Common.Configuration;

public class ConsulJsonConfigurationSource : IConfigurationSource
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;
    private readonly string _environment;
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public ConsulJsonConfigurationSource(
        IConsulClient consulClient,
        string serviceName,
        string environment,
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        _consulClient = consulClient;
        _serviceName = serviceName;
        _environment = environment;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        _logger.LogInformation("Building Consul JSON configuration provider for service {ServiceName} ({Environment})", 
            _serviceName, !string.IsNullOrEmpty(_environment) ? _environment : "default");
    
        var provider = new ConsulJsonConfigurationProvider(
            _consulClient, 
            _serviceName, 
            _environment, 
            _logger);
    
        if (_serviceProvider != null)
        {
            try
            {
                var lifetime = _serviceProvider.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime;
                var configuration = _serviceProvider.GetService(typeof(IConfiguration)) as IConfiguration;
            
                if (lifetime != null && configuration != null)
                {
                    var watcherLogger = _serviceProvider.GetService(typeof(ILogger<ConsulRealTimeConfigWatcher>)) as ILogger<ConsulRealTimeConfigWatcher>;
                
                    if (watcherLogger != null)
                    {
                        var watcher = new ConsulRealTimeConfigWatcher(
                            _consulClient,
                            _serviceName,
                            _environment,
                            watcherLogger,
                            configuration,
                            lifetime,
                            provider);

                        watcher.WatchEnvironmentSpecificConfig = true;
                        watcher.StartAsync(CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating real-time Consul config watcher");
            }
        }
    
        return provider;
    }
}