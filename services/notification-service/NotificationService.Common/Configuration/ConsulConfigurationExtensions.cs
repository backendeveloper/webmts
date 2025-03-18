using System.Text;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NotificationService.Common.Configuration;

public static class ConsulJsonConfigurationExtensions
{
    public static IConfigurationBuilder AddConsulJsonConfiguration(
        this IConfigurationBuilder builder,
        IConsulClient consulClient,
        string serviceName,
        string environment,
        ILogger logger,
        IServiceProvider serviceProvider = null)
    {
        return builder.Add(new ConsulJsonConfigurationSource(
            consulClient,
            serviceName,
            environment,
            logger,
            serviceProvider));
    }

    public static IServiceCollection AddConsulConfigurationMonitoring(this IServiceCollection services)
    {
        services.AddHostedService<ConsulConfigurationMonitor>();
        
        return services;
    }

    public static async Task SyncAppSettingsToConsulAsync(
        this IConfiguration configuration,
        IConsulClient consulClient,
        string serviceName,
        string environment,
        ILogger logger)
    {
        try
        {
            logger.LogInformation(
                "Syncing appsettings JSON files to Consul for service {ServiceName} (Env: {Environment})",
                serviceName, environment);

            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            var appSettingsPath = Path.Combine(basePath, "appsettings.json");
            if (File.Exists(appSettingsPath)) 
                await SyncJsonFileToConsulAsync(consulClient, serviceName, "appsettings.json", appSettingsPath, logger);

            if (!string.IsNullOrEmpty(environment))
            {
                var environmentSettingsPath = Path.Combine(basePath, $"appsettings.{environment}.json");
                if (File.Exists(environmentSettingsPath))
                {
                    var envFileName = $"appsettings.{environment}.json";
                    await SyncJsonFileToConsulAsync(consulClient, serviceName, envFileName, environmentSettingsPath,
                        logger);
                    logger.LogInformation("Ortam yapılandırması senkronize edildi: {EnvFile}", envFileName);
                }
                else
                    logger.LogWarning("Ortam yapılandırma dosyası bulunamadı: {EnvFile}",
                        $"appsettings.{environment}.json");
            }

            var syncCompletedKey = $"{serviceName}/config/_sync_completed";
            var syncCompletedPair = new KVPair(syncCompletedKey)
            {
                Value = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o"))
            };
            await consulClient.KV.Put(syncCompletedPair);

            logger.LogInformation("Yapılandırma dosyaları Consul'a başarıyla senkronize edildi");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing appsettings JSON files to Consul for service {ServiceName}",
                serviceName);
            throw;
        }
    }

    private static async Task SyncJsonFileToConsulAsync(
        IConsulClient consulClient,
        string serviceName,
        string configFileName,
        string filePath,
        ILogger logger)
    {
        try
        {
            var jsonContent = await File.ReadAllTextAsync(filePath);
            var consulKey = $"{serviceName}/config/{configFileName}";
            var kvPair = new KVPair(consulKey)
            {
                Value = Encoding.UTF8.GetBytes(jsonContent)
            };

            await consulClient.KV.Put(kvPair);

            logger.LogInformation("Configuration file {ConfigFile} synced to Consul for service {ServiceName}",
                configFileName, serviceName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error syncing {ConfigFile} to Consul for service {ServiceName}",
                configFileName, serviceName);
            throw;
        }
    }
}