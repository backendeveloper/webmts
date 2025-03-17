// using Consul;
//
// namespace AuthService.Client.Infrastructure.Consul;
//
// public class ConsulServiceRegistration(
//     IConsulClient consulClient,
//     ILogger<ConsulServiceRegistration> logger)
//     : IServiceRegistration, IDisposable
// {
//     private readonly List<string> _registeredServices = new();
//
//     public void RegisterService(ConsulRegistrationInfo registration)
//     {
//         try
//         {
//             var serviceCheck = new AgentServiceCheck
//             {
//                 HTTP =
//                     $"http://{registration.ServiceAddress}:{registration.ServicePort}/{registration.HealthCheckEndpoint}",
//                 Interval = TimeSpan.FromSeconds(30),
//                 Timeout = TimeSpan.FromSeconds(5),
//                 DeregisterCriticalServiceAfter = TimeSpan.FromMinutes(1)
//             };
//
//             var serviceRegistration = new AgentServiceRegistration
//             {
//                 ID = registration.ServiceId,
//                 Name = registration.ServiceName,
//                 Address = registration.ServiceAddress,
//                 Port = registration.ServicePort,
//                 Check = serviceCheck,
//                 Tags =
//                 [
//                     "webmts",
//                     "auth",
//                     "microservice",
//                     "api",
//                     "traefik.enable=true",
//                     $"traefik.http.routers.{registration.ServiceName}.rule=PathPrefix(`/api/auth`)",
//                     $"traefik.http.services.{registration.ServiceName}.loadbalancer.server.port={registration.ServicePort}"
//                 ]
//             };
//
//             consulClient.Agent.ServiceRegister(serviceRegistration).Wait();
//             _registeredServices.Add(registration.ServiceId);
//
//             logger.LogInformation("Service {ServiceName} with ID {ServiceId} registered successfully in Consul",
//                 registration.ServiceName, registration.ServiceId);
//
//             InitializeConsulConfig(registration.ServiceName).Wait();
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error registering service {ServiceName} in Consul", registration.ServiceName);
//             throw;
//         }
//     }
//
//     public void DeregisterService(string serviceId)
//     {
//         try
//         {
//             consulClient.Agent.ServiceDeregister(serviceId).Wait();
//             _registeredServices.Remove(serviceId);
//             logger.LogInformation("Service with ID {ServiceId} deregistered from Consul", serviceId);
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error deregistering service {ServiceId} from Consul", serviceId);
//         }
//     }
//
//     private async Task InitializeConsulConfig(string serviceName)
//     {
//         try
//         {
//             var existingConfig = await consulClient.KV.Get($"{serviceName}/config/initialized");
//             if (existingConfig.Response != null)
//             {
//                 logger.LogInformation("Configuration for {ServiceName} already exists in Consul", serviceName);
//                 return;
//             }
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/jwt/issuer")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes("WebmtsAuthService")
//             });
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/jwt/audience")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes("WebmtsClient")
//             });
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/service/name")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes(serviceName)
//             });
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/ratelimit/enabled")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes("true")
//             });
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/ratelimit/period")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes("60")
//             });
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/ratelimit/limit")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes("100")
//             });
//
//             await consulClient.KV.Put(new KVPair($"{serviceName}/config/initialized")
//             {
//                 Value = System.Text.Encoding.UTF8.GetBytes("true")
//             });
//
//             logger.LogInformation("Initial configuration for {ServiceName} stored in Consul", serviceName);
//         }
//         catch (Exception ex)
//         {
//             logger.LogError(ex, "Error storing initial configuration for {ServiceName} in Consul", serviceName);
//         }
//     }
//
//     public void Dispose()
//     {
//         foreach (var serviceId in _registeredServices.ToList()) 
//             DeregisterService(serviceId);
//
//         consulClient?.Dispose();
//     }
// }