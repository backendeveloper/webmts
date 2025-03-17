using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public static class ConsulConfigurationExtensions
{
    public static IConfigurationBuilder AddConsul(
        this IConfigurationBuilder builder,
        IConsulClient consulClient,
        string serviceName,
        ILogger logger)
    {
        return builder.Add(new ConsulConfigurationSource(consulClient, serviceName, logger));
    }
}