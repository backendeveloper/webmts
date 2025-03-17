namespace AuthService.Common.ServiceDiscovery.Consul;

public class ConsulConfig
{
    public string Host { get; set; } = "consul";
    public int Port { get; set; } = 8500;
    public string Scheme { get; set; } = "http";
}