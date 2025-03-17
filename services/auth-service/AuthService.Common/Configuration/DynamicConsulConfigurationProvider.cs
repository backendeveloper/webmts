using System.Collections.Concurrent;
using AuthService.Common.ServiceDiscovery.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public class DynamicConsulConfigurationProvider : ConfigurationProvider, IDisposable
{
    private readonly IKeyValueStore _keyValueStore;
    private readonly string _serviceName;
    private readonly ILogger _logger;
    private readonly Timer _pollingTimer;
    private readonly ConcurrentDictionary<string, string> _consulValues = new();
    private readonly CancellationTokenSource _cts = new();

    public DynamicConsulConfigurationProvider(
        IKeyValueStore keyValueStore,
        string serviceName,
        ILogger logger,
        TimeSpan pollingInterval)
    {
        _keyValueStore = keyValueStore;
        _serviceName = serviceName;
        _logger = logger;

        // Periyodik olarak Consul'u kontrol eden zamanlayıcı
        _pollingTimer = new Timer(
            _ => CheckForConfigurationChangesAsync().ConfigureAwait(false).GetAwaiter().GetResult(),
            null,
            TimeSpan.FromSeconds(10), // İlk kontrol gecikmesi
            pollingInterval);  // Düzenli kontrol aralığı
    }

    public override void Load()
    {
        LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        try
        {
            var consulValues = await _keyValueStore.GetAllValuesAsync($"{_serviceName}/config");
            foreach (var kvp in consulValues)
            {
                // Consul key path'i temizle
                var key = kvp.Key.Replace($"{_serviceName}/config/", "").Replace("/", ":");
                
                Data[key] = kvp.Value;
                _consulValues[key] = kvp.Value;
            }

            _logger.LogInformation("Loaded {Count} configuration values from Consul", consulValues.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from Consul");
        }
    }

    private async Task CheckForConfigurationChangesAsync()
    {
        try
        {
            var hasChanges = false;
            var consulValues = await _keyValueStore.GetAllValuesAsync($"{_serviceName}/config");

            // Yeni veya değişmiş değerleri kontrol et
            foreach (var kvp in consulValues)
            {
                var key = kvp.Key.Replace($"{_serviceName}/config/", "").Replace("/", ":");
                
                if (!_consulValues.TryGetValue(key, out var currentValue) || currentValue != kvp.Value)
                {
                    _consulValues[key] = kvp.Value;
                    Data[key] = kvp.Value;
                    hasChanges = true;
                }
            }

            // Silinen değerleri kontrol et
            var keysToRemove = _consulValues.Keys
                .Where(k => !consulValues.Keys.Any(ck => 
                    ck.Replace($"{_serviceName}/config/", "").Replace("/", ":") == k))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _consulValues.TryRemove(key, out _);
                Data.Remove(key);
                hasChanges = true;
            }

            // Değişiklik varsa reload tetikle
            if (hasChanges)
            {
                _logger.LogInformation("Configuration changes detected in Consul, triggering reload");
                OnReload();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for configuration changes in Consul");
        }
    }

    public void Dispose()
    {
        _pollingTimer?.Dispose();
        _cts.Cancel();
        _cts.Dispose();
    }
}