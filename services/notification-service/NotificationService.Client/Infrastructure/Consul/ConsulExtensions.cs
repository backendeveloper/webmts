using Microsoft.Extensions.Options;
using NotificationService.Common.ServiceDiscovery.Interfaces;

namespace NotificationService.Client.Infrastructure.Consul;

public static class ConsulExtensions
{
    public static IApplicationBuilder UseConsul(this IApplicationBuilder app)
    {
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        var serviceDiscovery = app.ApplicationServices.GetRequiredService<IServiceDiscovery>();
        var serviceConfig = app.ApplicationServices
            .GetRequiredService<IOptions<NotificationService.Common.ServiceDiscovery.Consul.ServiceConfig>>().Value;
        var logger = app.ApplicationServices.GetRequiredService<ILogger<IApplicationBuilder>>();

        var serviceId = $"{serviceConfig.Name}-{Guid.NewGuid()}";

        lifetime.ApplicationStarted.Register(async () =>
        {
            try
            {
                logger.LogInformation("Servis kaydediliyor: {ServiceName} (ID: {ServiceId})",
                    serviceConfig.Name, serviceId);

                await serviceDiscovery.RegisterServiceAsync(serviceConfig.Name, serviceId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Servis kaydı sırasında hata oluştu");
            }
        });

        lifetime.ApplicationStopping.Register(async () =>
        {
            try
            {
                logger.LogInformation("Servis kaydı siliniyor: {ServiceName} (ID: {ServiceId})",
                    serviceConfig.Name, serviceId);

                await serviceDiscovery.DeregisterServiceAsync(serviceId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Servis kaydı silinirken hata oluştu");
            }
        });

        return app;
    }
}