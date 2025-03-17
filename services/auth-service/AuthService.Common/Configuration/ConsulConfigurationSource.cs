using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public class ConsulConfigurationSource : IConfigurationSource
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;
    private readonly ILogger _logger;

    public ConsulConfigurationSource(IConsulClient consulClient, string serviceName, ILogger logger)
    {
        _consulClient = consulClient;
        _serviceName = serviceName;
        _logger = logger;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new ConsulConfigurationProvider(_consulClient, _serviceName, _logger);
    }
}