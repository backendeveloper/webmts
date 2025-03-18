namespace TransactionService.Common.ServiceDiscovery.Consul;

public class ServiceConfig
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int Port { get; set; } = 8080;
    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
    public int DeregisterAfterMinutes { get; set; } = 1;
    public string HealthCheckEndpoint { get; set; } = "health";
    public string[] Tags { get; set; } = [];
}