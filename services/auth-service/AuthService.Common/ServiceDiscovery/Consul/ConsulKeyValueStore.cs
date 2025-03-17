using System.Text;
using System.Text.Json;
using AuthService.Common.ServiceDiscovery.Interfaces;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.ServiceDiscovery.Consul;

public class ConsulKeyValueStore : IKeyValueStore
{
    private readonly IConsulClient _consulClient;
    private readonly ILogger<ConsulKeyValueStore> _logger;
    private readonly IConfiguration _configuration;

    public ConsulKeyValueStore(
        IConsulClient consulClient,
        ILogger<ConsulKeyValueStore> logger,
        IConfiguration configuration = null)
    {
        _consulClient = consulClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> SetValueAsync(string key, string value)
    {
        try
        {
            var kvPair = new KVPair(key)
            {
                Value = Encoding.UTF8.GetBytes(value)
            };

            var result = await _consulClient.KV.Put(kvPair);
            _logger.LogDebug("Set value in Consul: {Key} = {Value}", key, value);
            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set value for key {Key} in Consul", key);
            return false;
        }
    }

    public async Task<bool> SetValueAsync<T>(string key, T value) where T : class
    {
        if (value == null)
            return false;

        try
        {
            var json = JsonSerializer.Serialize(value);
            return await SetValueAsync(key, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to serialize and set object for key {Key} in Consul", key);
            return false;
        }
    }

    public async Task<string> GetValueAsync(string key)
    {
        try
        {
            var getPair = await _consulClient.KV.Get(key);
            if (getPair.Response != null && getPair.Response.Value != null)
            {
                var value = Encoding.UTF8.GetString(getPair.Response.Value);
                _logger.LogDebug("Retrieved value from Consul: {Key} = {Value}", key, value);
                return value;
            }

            _logger.LogDebug("No value found in Consul for key: {Key}", key);
            
            // Eğer configuration verilmişse ve değer Consul'da yoksa, configuration'dan almayı dene
            if (_configuration != null)
            {
                var configKey = ConvertConsulKeyToConfigKey(key);
                var configValue = _configuration[configKey];
                
                if (!string.IsNullOrEmpty(configValue))
                {
                    _logger.LogDebug("Using value from configuration for key {Key} = {Value}", key, configValue);
                    return configValue;
                }
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving value for key {Key} from Consul", key);
            return null;
        }
    }

    public async Task<T> GetValueAsync<T>(string key) where T : class
    {
        try
        {
            var json = await GetValueAsync(key);
            if (string.IsNullOrEmpty(json))
                return null;

            return JsonSerializer.Deserialize<T>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deserializing value for key {Key} from Consul", key);
            return null;
        }
    }

    public async Task<bool> DeleteValueAsync(string key)
    {
        try
        {
            var result = await _consulClient.KV.Delete(key);
            _logger.LogDebug("Deleted key from Consul: {Key}", key);
            return result.Response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key {Key} from Consul", key);
            return false;
        }
    }

    public async Task<IDictionary<string, string>> GetAllValuesAsync(string prefix)
    {
        var result = new Dictionary<string, string>();

        try
        {
            var consulResult = await _consulClient.KV.List(prefix);
            if (consulResult.Response != null)
            {
                foreach (var pair in consulResult.Response)
                {
                    if (pair.Value != null)
                    {
                        result[pair.Key] = Encoding.UTF8.GetString(pair.Value);
                    }
                }
            }

            _logger.LogDebug("Retrieved {Count} values from Consul with prefix {Prefix}", result.Count, prefix);
            
            // Eğer Consul'dan hiç değer gelmezse ve configuration verilmişse, konfigürasyondan almayı dene
            if (result.Count == 0 && _configuration != null)
            {
                var section = GetConfigurationSectionFromPrefix(prefix);
                if (section != null)
                {
                    _logger.LogInformation("No values found in Consul for prefix {Prefix}, checking configuration", prefix);
                    var configValues = FlattenConfigurationSection(section, prefix);
                    foreach (var kvp in configValues)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving values with prefix {Prefix} from Consul", prefix);
        }

        return result;
    }

    public async Task<bool> SyncConfigurationSectionToConsulAsync(string sectionName, string consulPrefix)
    {
        if (_configuration == null)
        {
            _logger.LogWarning("Cannot sync configuration to Consul: No configuration was provided");
            return false;
        }

        try
        {
            var section = _configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                _logger.LogWarning("Configuration section {SectionName} does not exist", sectionName);
                return false;
            }

            _logger.LogInformation("Syncing configuration section {SectionName} to Consul at {ConsulPrefix}", 
                sectionName, consulPrefix);

            // Senkronizasyon başlangıcını kontrol et
            var initialized = await GetValueAsync($"{consulPrefix}/initialized");
            if (initialized == "true")
            {
                _logger.LogInformation("Configuration section {SectionName} is already initialized in Consul", sectionName);
                return true;
            }

            // Konfigürasyon değerlerini düzleştir
            var values = FlattenConfigurationSection(section);
            
            // Her değeri Consul'a ekle
            foreach (var kvp in values)
            {
                var consulKey = $"{consulPrefix}/{kvp.Key.Replace(":", "/")}";
                await SetValueAsync(consulKey, kvp.Value);
            }

            // Senkronizasyonun tamamlandığını işaretle
            await SetValueAsync($"{consulPrefix}/initialized", "true");
            
            _logger.LogInformation("Successfully synced {Count} values from configuration section {SectionName} to Consul", 
                values.Count, sectionName);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing configuration section {SectionName} to Consul", sectionName);
            return false;
        }
    }

    public async Task<bool> SyncAllConfigurationsToConsulAsync(string serviceName)
    {
        if (_configuration == null)
        {
            _logger.LogWarning("Cannot sync configurations to Consul: No configuration was provided");
            return false;
        }

        try
        {
            _logger.LogInformation("Starting full configuration synchronization to Consul for service {ServiceName}", 
                serviceName);

            // Önce global senkronizasyon durumunu kontrol et
            var fullySynced = await GetValueAsync($"{serviceName}/config/fully_synced");
            if (fullySynced == "true")
            {
                _logger.LogInformation("All configurations are already synced to Consul for service {ServiceName}", 
                    serviceName);
                return true;
            }

            // Tüm birinci seviye konfigürasyon bölümlerini senkronize et
            var children = _configuration.GetChildren();
            foreach (var child in children)
            {
                // Özel durumlar: ConnectionStrings, Logging gibi bölümleri atla
                if (child.Key == "ConnectionStrings" || child.Key == "Logging" || 
                    child.Key == "AllowedHosts")
                    continue;

                // Bölümü Consul'a senkronize et
                await SyncConfigurationSectionToConsulAsync(
                    child.Key, 
                    $"{serviceName}/config/{child.Key.ToLowerInvariant()}");
            }

            // Bağlantı dizelerini ayrıca senkronize et
            var connectionStrings = _configuration.GetSection("ConnectionStrings");
            if (connectionStrings.Exists())
            {
                await SyncConfigurationSectionToConsulAsync(
                    "ConnectionStrings", 
                    $"{serviceName}/config/connectionstrings");
            }

            // Tam senkronizasyonu işaretle
            await SetValueAsync($"{serviceName}/config/fully_synced", "true");
            
            _logger.LogInformation("Successfully synced all configurations to Consul for service {ServiceName}", 
                serviceName);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all configurations to Consul for service {ServiceName}", serviceName);
            return false;
        }
    }

    private string ConvertConsulKeyToConfigKey(string consulKey)
    {
        // Örnek: "auth-service/config/jwt/issuer" -> "jwt:issuer"
        var parts = consulKey.Split('/');
        
        // En az 3 parça bekle: service/config/section
        if (parts.Length < 3)
            return consulKey.Replace("/", ":");
            
        // Servis adı ve config kısmını atla
        var configPath = parts.Skip(2).ToArray();
        return string.Join(":", configPath);
    }

    private IConfigurationSection GetConfigurationSectionFromPrefix(string prefix)
    {
        if (_configuration == null)
            return null;
            
        // Örnek: "auth-service/config/jwt" -> configuration.GetSection("jwt")
        var parts = prefix.Split('/').ToList();
        
        // En az service/config/section formatını bekle
        if (parts.Count < 3)
            return null;
            
        // Servis ve config kısmını atla, section adını al
        return _configuration.GetSection(parts[2]);
    }

    private Dictionary<string, string> FlattenConfigurationSection(IConfigurationSection section, string keyPrefix = "")
    {
        var result = new Dictionary<string, string>();
        
        // Alt bölümleri ve değerleri düzleştir
        FlattenSection(section, "", result);
        
        // Eğer key prefix verilmişse, anahtarları düzenle
        if (!string.IsNullOrEmpty(keyPrefix))
        {
            return result.ToDictionary(
                kvp => $"{keyPrefix}/{kvp.Key}",
                kvp => kvp.Value);
        }
        
        return result;
    }

    private void FlattenSection(IConfiguration config, string keyPrefix, Dictionary<string, string> result)
    {
        foreach (var child in config.GetChildren())
        {
            var key = string.IsNullOrEmpty(keyPrefix) ? child.Key : $"{keyPrefix}:{child.Key}";
            
            if (child.Value != null)
            {
                result[key] = child.Value;
            }
            else
            {
                FlattenSection(child, key, result);
            }
        }
    }
}