// using System.Net;
// using Consul;
//
// namespace AuthService.Client.Infrastructure.Consul;
//
// public static class ConsulExtensions
// {
//     public static IServiceCollection AddConsulServices(this IServiceCollection services, IConfiguration configuration)
//     {
//         services.AddSingleton<IConsulClient>(_ => new ConsulClient(config =>
//         {
//             var consulHost = configuration["Consul:Host"] ?? "consul";
//             var consulPort = int.Parse(configuration["Consul:Port"] ?? "8500");
//             config.Address = new Uri($"http://{consulHost}:{consulPort}");
//         }));
//
//         services.AddSingleton<IServiceRegistration, ConsulServiceRegistration>();
//         services.AddHostedService<ConsulHostedService>();
//
//         return services;
//     }
//
//     public static IApplicationBuilder UseConsul(this IApplicationBuilder app, IConfiguration configuration)
//     {
//         var consulClient = app.ApplicationServices.GetRequiredService<IConsulClient>();
//         var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
//         var serviceRegistration = app.ApplicationServices.GetRequiredService<IServiceRegistration>();
//
//         var serviceName = configuration["ServiceSettings:ServiceName"] ?? "auth-service";
//         var serviceId = $"{serviceName}-{Guid.NewGuid()}";
//         var serviceAddress = configuration["ServiceSettings:ServiceAddress"] ?? GetHostAddress() ?? "auth-service";
//         var servicePort = int.Parse(configuration["ServiceSettings:ServicePort"] ?? "8080");
//
//         serviceRegistration.RegisterService(new ConsulRegistrationInfo
//         {
//             ServiceId = serviceId,
//             ServiceName = serviceName,
//             ServiceAddress = serviceAddress,
//             ServicePort = servicePort,
//             HealthCheckEndpoint = "api/auth/health"
//         });
//
//         return app;
//     }
//
//     private static string GetHostAddress()
//     {
//         try
//         {
//             var hostName = Dns.GetHostName();
//             var addresses = Dns.GetHostAddresses(hostName);
//             
//             var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
//             if (ipv4 != null)
//                 return ipv4.ToString();
//             
//             return addresses.FirstOrDefault()?.ToString() ?? hostName;
//         }
//         catch
//         {
//             return null;
//         }
//     }
// }