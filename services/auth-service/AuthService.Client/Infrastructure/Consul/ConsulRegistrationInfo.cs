namespace AuthService.Client.Infrastructure.Consul;

public class ConsulRegistrationInfo
{
    public string ServiceId { get; set; }
    public string ServiceName { get; set; }
    public string ServiceAddress { get; set; }
    public int ServicePort { get; set; }
    public string HealthCheckEndpoint { get; set; }
}