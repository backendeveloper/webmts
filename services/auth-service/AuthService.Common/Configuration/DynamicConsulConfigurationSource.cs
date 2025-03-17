using AuthService.Common.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public class DynamicConsulConfigurationSource : IConfigurationSource
{
    private readonly IKeyValueStore _keyValueStore;
    private readonly string _serviceName;
    private readonly ILogger _logger;
    private readonly TimeSpan _pollingInterval;

    public DynamicConsulConfigurationSource(
        IKeyValueStore keyValueStore,
        string serviceName,
        ILogger logger,
        TimeSpan pollingInterval)
    {
        _keyValueStore = keyValueStore;
        _serviceName = serviceName;
        _logger = logger;
        _pollingInterval = pollingInterval;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new DynamicConsulConfigurationProvider(
            _keyValueStore, 
            _serviceName, 
            _logger, 
            _pollingInterval);
    }
}