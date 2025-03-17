using AuthService.Common.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public class ConfigurationSynchronizer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IKeyValueStore _keyValueStore;
    private readonly ILogger<ConfigurationSynchronizer> _logger;
    private readonly string _serviceName;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5);

    // Consul'a senkronize edilecek önemli bölümler
    private static readonly string[] ImportantSections =
    {
        "Jwt",
        "TokenSettings",
        "RateLimit",
        "Service",
        "FeatureFlags"
    };

    public ConfigurationSynchronizer(
        IConfiguration configuration,
        IKeyValueStore keyValueStore,
        ILogger<ConfigurationSynchronizer> logger)
    {
        _configuration = configuration;
        _keyValueStore = keyValueStore;
        _logger = logger;
        _serviceName = _configuration["Service:Name"] ?? "auth-service";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // İlk çalıştırmada tam senkronizasyon yap
            await PerformInitialSyncAsync();

            // Periyodik senkronizasyon döngüsü
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_syncInterval, stoppingToken);
                    await CheckForConfigChangesAsync();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during periodic configuration check");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in configuration synchronizer");
        }
    }

    private async Task PerformInitialSyncAsync()
    {
        try
        {
            _logger.LogInformation("Starting initial configuration synchronization for service {ServiceName}",
                _serviceName);

            // Önce Consul'dan global senkronizasyon durumunu kontrol et
            var synced = await _keyValueStore.GetValueAsync($"{_serviceName}/config/initialized");
            if (synced == "true")
            {
                _logger.LogInformation("Configuration already initialized in Consul for service {ServiceName}",
                    _serviceName);
                return;
            }

            _logger.LogInformation("Initializing configuration in Consul for service {ServiceName}", _serviceName);

            // Önce önemli bölümleri senkronize et
            foreach (var section in ImportantSections)
            {
                if (_configuration.GetSection(section).Exists())
                {
                    await _keyValueStore.SyncConfigurationSectionToConsulAsync(
                        section,
                        $"{_serviceName}/config/{section.ToLowerInvariant()}");
                }
            }

            // Senkronizasyonu işaretle
            await _keyValueStore.SetValueAsync($"{_serviceName}/config/initialized", "true");
            await _keyValueStore.SetValueAsync($"{_serviceName}/config/last_sync", DateTime.UtcNow.ToString("o"));

            _logger.LogInformation("Initial configuration synchronization completed for service {ServiceName}",
                _serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initial configuration synchronization");
        }
    }

    private async Task CheckForConfigChangesAsync()
    {
        try
        {
            _logger.LogDebug("Checking for configuration changes in Consul");

            // Consul'dan son senkronizasyon zamanını al
            var lastSyncStr = await _keyValueStore.GetValueAsync($"{_serviceName}/config/last_sync");
            if (string.IsNullOrEmpty(lastSyncStr))
            {
                // Son senkronizasyon bilgisi yoksa tam senkronizasyon yap
                await PerformInitialSyncAsync();
                return;
            }

            // Son senkronizasyon zamanını parse et
            if (DateTime.TryParse(lastSyncStr, out var lastSync))
            {
                // Son senkronizasyondan bu yana çok zaman geçmişse (örn. 1 gün) tam senkronizasyon yap
                if (DateTime.UtcNow - lastSync > TimeSpan.FromDays(1))
                {
                    _logger.LogInformation("Last sync was over a day ago, performing full sync");
                    await _keyValueStore.SyncAllConfigurationsToConsulAsync(_serviceName);
                    await _keyValueStore.SetValueAsync($"{_serviceName}/config/last_sync",
                        DateTime.UtcNow.ToString("o"));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for configuration changes");
        }
    }
}