using AuthService.Common.ServiceDiscovery.Consul;

namespace AuthService.Common.ServiceDiscovery.Interfaces;

public interface IServiceDiscovery
{
    Task RegisterServiceAsync(string serviceName, string serviceId = null);
    Task DeregisterServiceAsync(string serviceId);
    Task<IEnumerable<ServiceInfo>> GetServicesAsync(string serviceName = null);
}