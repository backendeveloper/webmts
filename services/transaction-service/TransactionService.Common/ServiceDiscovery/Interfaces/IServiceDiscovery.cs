namespace TransactionService.Common.ServiceDiscovery.Interfaces;

public interface IServiceDiscovery
{
    Task RegisterServiceAsync(string serviceName, string serviceId = null);
    Task DeregisterServiceAsync(string serviceId);
}