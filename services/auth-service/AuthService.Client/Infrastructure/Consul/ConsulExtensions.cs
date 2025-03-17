using Consul;

namespace AuthService.Client.Infrastructure.Consul;

public static class ConsulExtensions
{
    public static IServiceCollection AddConsulServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IConsulClient>(provider => new ConsulClient(config =>
        {
            var consulHost = configuration["Consul:Host"] ?? "consul";
            var consulPort = int.Parse(configuration["Consul:Port"] ?? "8500");
            config.Address = new Uri($"http://{consulHost}:{consulPort}");
        }));

        services.AddSingleton<IServiceRegistration, ConsulServiceRegistration>();

        return services;
    }
}