using AuthService.Common.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public static class DynamicConsulConfigurationExtensions
{
    public static IConfigurationBuilder AddDynamicConsul(
        this IConfigurationBuilder builder,
        IKeyValueStore keyValueStore,
        string serviceName,
        ILogger logger,
        TimeSpan? pollingInterval = null)
    {
        return builder.Add(new DynamicConsulConfigurationSource(
            keyValueStore, 
            serviceName, 
            logger, 
            pollingInterval ?? TimeSpan.FromMinutes(1)));
    }
}