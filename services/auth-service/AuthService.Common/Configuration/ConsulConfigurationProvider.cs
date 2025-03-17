using System.Text;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthService.Common.Configuration;

public class ConsulConfigurationProvider : ConfigurationProvider
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;
    private readonly ILogger _logger;

    public ConsulConfigurationProvider(IConsulClient consulClient, string serviceName, ILogger logger)
    {
        _consulClient = consulClient;
        _serviceName = serviceName;
        _logger = logger;
    }

    public override void Load()
    {
        LoadAsync().Wait();
    }

    private async Task LoadAsync()
    {
        try
        {
            var pairs = await _consulClient.KV.List($"{_serviceName}/config");
            if (pairs.Response == null)
            {
                _logger.LogWarning("No configuration found in Consul for {ServiceName}", _serviceName);
                return;
            }

            foreach (var pair in pairs.Response)
            {
                if (pair.Value == null)
                    continue;

                // Consul anahtar yolunu .NET yapılandırma anahtarına dönüştür
                var key = pair.Key
                    .Replace($"{_serviceName}/config/", string.Empty)
                    .Replace("/", ":");

                var value = Encoding.UTF8.GetString(pair.Value);
                Data[key] = value;
                
                _logger.LogDebug("Loaded configuration from Consul: {Key} = {Value}", key, value);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from Consul for {ServiceName}", _serviceName);
        }
    }
}
