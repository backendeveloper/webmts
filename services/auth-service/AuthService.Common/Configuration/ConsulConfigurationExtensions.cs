using System.Text;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

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

    public static async Task SyncAppSettingsToConsulAsync(
        this IConfiguration configuration,
        IConsulClient consulClient,
        string serviceName,
        string environment,
        ILogger logger)
    {
        try
        {
            logger.LogInformation("Syncing appsettings JSON files to Consul for service {ServiceName}", serviceName);

            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            var appSettingsPath = Path.Combine(basePath, "appsettings.json");
            if (File.Exists(appSettingsPath))
            {
                await SyncJsonFileToConsulAsync(consulClient, serviceName, "appsettings.json", appSettingsPath, logger);
            }

            if (!string.IsNullOrEmpty(environment))
            {
                var environmentSettingsPath = Path.Combine(basePath, $"appsettings.{environment}.json");
                if (File.Exists(environmentSettingsPath))
                {
                    await SyncJsonFileToConsulAsync(consulClient, serviceName, $"appsettings.{environment}.json",
                        environmentSettingsPath, logger);
                }
            }

            var environments = new[] { "Development", "Staging", "Production" };
            foreach (var env in environments.Where(e => e != environment))
            {
                var envSettingsPath = Path.Combine(basePath, $"appsettings.{env}.json");
                if (File.Exists(envSettingsPath))
                {
                    await SyncJsonFileToConsulAsync(consulClient, serviceName, $"appsettings.{env}.json",
                        envSettingsPath, logger);
                }
            }

            await consulClient.KV.Put(new KVPair($"{serviceName}/config/_sync_completed")
            {
                Value = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("o"))
            });

            logger.LogInformation("Completed syncing appsettings JSON files to Consul for service {ServiceName}",
                serviceName);
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