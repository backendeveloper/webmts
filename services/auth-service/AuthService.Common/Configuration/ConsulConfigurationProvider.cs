using System.Text;
using System.Text.Json;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public class ConsulJsonConfigurationProvider : ConfigurationProvider
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;
    private readonly string _environment;
    private readonly ILogger _logger;
    private readonly Dictionary<string, string> _loadedJsons = new();

    public ConsulJsonConfigurationProvider(
        IConsulClient consulClient,
        string serviceName,
        string environment,
        ILogger logger)
    {
        _consulClient = consulClient;
        _serviceName = serviceName;
        _environment = environment;
        _logger = logger;
    }

    public override void Load()
    {
        LoadAsync().ConfigureAwait(false).GetAwaiter().GetResult();
    }

    private async Task LoadAsync()
    {
        try
        {
            await LoadConfigFileAsync("appsettings.json");
            if (!string.IsNullOrEmpty(_environment))
                await LoadConfigFileAsync($"appsettings.{_environment}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading JSON configuration from Consul for service {ServiceName}",
                _serviceName);
        }
    }

    public async Task LoadConfigFileAsync(string configFileName)
    {
        try
        {
            var consulKey = $"{_serviceName}/config/{configFileName}";
            var getPair = await _consulClient.KV.Get(consulKey);
            if (getPair.Response == null || getPair.Response.Value == null)
            {
                _logger.LogWarning("Configuration file {ConfigFile} not found in Consul for service {ServiceName}",
                    configFileName, _serviceName);
                return;
            }

            var jsonContent = Encoding.UTF8.GetString(getPair.Response.Value);
            await LoadConfigFileAsync(configFileName, jsonContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading {ConfigFile} from Consul for service {ServiceName}",
                configFileName, _serviceName);
        }
    }

    public async Task LoadConfigFileAsync(string configFileName, string jsonContent)
    {
        try
        {
            _loadedJsons[configFileName] = jsonContent;
            using var jsonDoc = JsonDocument.Parse(jsonContent);
            FlattenJson(jsonDoc.RootElement, string.Empty);

            _logger.LogInformation("Loaded configuration from {ConfigFile} in Consul for service {ServiceName}",
                configFileName, _serviceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing JSON content for {ConfigFile} in service {ServiceName}",
                configFileName, _serviceName);
        }
    }

    private void FlattenJson(JsonElement element, string path)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var propertyPath = string.IsNullOrEmpty(path)
                        ? property.Name
                        : $"{path}:{property.Name}";

                    FlattenJson(property.Value, propertyPath);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var arrayPath = $"{path}:{index}";
                    FlattenJson(item, arrayPath);
                    index++;
                }

                break;

            default:
                var value = element.ToString();
                Data[path] = value;
                break;
        }
    }

    public void TriggerReload() =>
        OnReload();
}