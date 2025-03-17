// namespace AuthService.Client.Infrastructure.Consul;
//
// public class ConsulHostedService : IHostedService
// {
//     private readonly IServiceRegistration _serviceRegistration;
//     private readonly IConfiguration _configuration;
//     private readonly ILogger<ConsulHostedService> _logger;
//     private string _serviceId;
//
//     public ConsulHostedService(
//         IServiceRegistration serviceRegistration,
//         IConfiguration configuration,
//         ILogger<ConsulHostedService> logger)
//     {
//         _serviceRegistration = serviceRegistration;
//         _configuration = configuration;
//         _logger = logger;
//     }
//
//     public Task StartAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Consul Hosted Service starting...");
//         return Task.CompletedTask;
//     }
//
//     public Task StopAsync(CancellationToken cancellationToken)
//     {
//         _logger.LogInformation("Consul Hosted Service stopping...");
//         
//         if (!string.IsNullOrEmpty(_serviceId))
//         {
//             _logger.LogInformation("Deregistering service from Consul: {ServiceId}", _serviceId);
//             _serviceRegistration.DeregisterService(_serviceId);
//         }
//         
//         return Task.CompletedTask;
//     }
// }