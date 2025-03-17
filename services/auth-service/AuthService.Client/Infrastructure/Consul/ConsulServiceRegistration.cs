using Consul;

namespace AuthService.Client.Infrastructure.Consul;

public class ConsulServiceRegistration : IServiceRegistration, IDisposable
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulServiceRegistration> _logger;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly List<string> _registeredServices = new List<string>();

    public ConsulServiceRegistration(IConsulClient consulClient, ILogger<ConsulServiceRegistration> logger, IHostApplicationLifetime appLifetime)
    {
        _consulClient = consulClient;
        _logger = logger;
        _appLifetime = appLifetime;

        // Uygulama kapanırken tüm kayıtlı servisleri kaldır
        _appLifetime.ApplicationStopping.Register(() =>
        {
            foreach (var serviceId in _registeredServices)
            {
                DeregisterService(serviceId);
            }
        });
    }

    public void RegisterService(ConsulRegistrationInfo registration)
    {
        var serviceCheck = new AgentServiceCheck
        {
            HTTP = $"http://{registration.ServiceAddress}:{registration.ServicePort}/{registration.HealthCheckEndpoint}",
            Interval = TimeSpan.FromSeconds(30),
            Timeout = TimeSpan.FromSeconds(5),
            DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
        };

        var serviceRegistration = new AgentServiceRegistration
        {
            ID = registration.ServiceId,
            Name = registration.ServiceName,
            Address = registration.ServiceAddress,
            Port = registration.ServicePort,
            Check = serviceCheck,
            Tags = new[] { "webmts", "auth", "microservice" }
        };

        try
        {
            _consulClient.Agent.ServiceRegister(serviceRegistration).Wait();
            _registeredServices.Add(registration.ServiceId);
            _logger.LogInformation("Service {ServiceName} with ID {ServiceId} registered successfully in Consul", 
                registration.ServiceName, registration.ServiceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service {ServiceName} in Consul", registration.ServiceName);
        }
    }

    public void DeregisterService(string serviceId)
    {
        try
        {
            _consulClient.Agent.ServiceDeregister(serviceId).Wait();
            _registeredServices.Remove(serviceId);
            _logger.LogInformation("Service with ID {ServiceId} deregistered from Consul", serviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deregistering service {ServiceId} from Consul", serviceId);
        }
    }

    public void Dispose()
    {
        foreach (var serviceId in _registeredServices.ToList())
        {
            DeregisterService(serviceId);
        }
        
        _consulClient?.Dispose();
    }
}