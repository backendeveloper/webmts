using Consul;
using CustomerService.Common.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CustomerService.Common.ServiceDiscovery.Consul;

public class ConsulServiceDiscovery : IServiceDiscovery
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceDiscovery> _logger;
    private readonly ServiceConfig _serviceConfig;

    public ConsulServiceDiscovery(
        IConsulClient consulClient,
        IOptions<ServiceConfig> serviceConfig,
        ILogger<ConsulServiceDiscovery> logger)
    {
        _consulClient = consulClient;
        _logger = logger;
        _serviceConfig = serviceConfig.Value;
    }

    public async Task RegisterServiceAsync(string serviceName, string serviceId = null)
    {
        var serviceCheck = new AgentServiceCheck
        {
            HTTP = $"http://{_serviceConfig.Address}:{_serviceConfig.Port}/{_serviceConfig.HealthCheckEndpoint}",
            Interval = TimeSpan.FromSeconds(_serviceConfig.HealthCheckIntervalSeconds),
            Timeout = TimeSpan.FromSeconds(_serviceConfig.HealthCheckTimeoutSeconds),
            DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(_serviceConfig.DeregisterAfterMinutes)
        };

        var tags = _serviceConfig.Tags.ToList();
        tags.AddRange([
            "webmts",
            "microservice",
            "api",
            "traefik.enable=true",
            $"traefik.http.routers.{serviceName}.rule=PathPrefix(`/api/{serviceName}`)",
            $"traefik.http.services.{serviceName}.loadbalancer.server.port={_serviceConfig.Port}"
        ]);

        var registration = new AgentServiceRegistration
        {
            ID = serviceId,
            Name = serviceName,
            Address = _serviceConfig.Address,
            Port = _serviceConfig.Port,
            Tags = tags.ToArray(),
            Check = serviceCheck
        };

        await _consulClient.Agent.ServiceRegister(registration);
    }

    public async Task DeregisterServiceAsync(string serviceId)
    {
        await _consulClient.Agent.ServiceDeregister(serviceId);
    }
}