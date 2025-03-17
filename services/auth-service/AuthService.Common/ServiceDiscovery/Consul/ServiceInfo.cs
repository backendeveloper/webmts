namespace AuthService.Common.ServiceDiscovery.Consul;

public class ServiceInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public int Port { get; set; }
    public string[] Tags { get; set; }
    public bool Healthy { get; set; }
}