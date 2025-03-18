using System.Text;
using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Common.Configuration;

public class ConsulRealTimeConfigWatcher : BackgroundService
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;
    private readonly string _environment;
    private readonly ILogger<ConsulRealTimeConfigWatcher> _logger;
    private readonly IConfiguration _configuration;
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ConsulJsonConfigurationProvider _configProvider;
    public bool WatchEnvironmentSpecificConfig { get; set; }

    private ulong _lastIndex = 0;

    public ConsulRealTimeConfigWatcher(
        IConsulClient consulClient,
        string serviceName,
        string environment,
        ILogger<ConsulRealTimeConfigWatcher> logger,
        IConfiguration configuration,
        IHostApplicationLifetime appLifetime,
        ConsulJsonConfigurationProvider configProvider)
    {
        _consulClient = consulClient;
        _serviceName = serviceName;
        _environment = environment;
        _logger = logger;
        _configuration = configuration;
        _appLifetime = appLifetime;
        _configProvider = configProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        _logger.LogInformation("Consul gerçek zamanlı yapılandırma izleyicisi başlatıldı");

        var configIndexes = new Dictionary<string, ulong>();

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var files = new List<string> { "appsettings.json" };
                    if (!string.IsNullOrEmpty(_environment))
                    {
                        files.Add($"appsettings.{_environment}.json");
                        _logger.LogDebug("İzlenen environment dosyası: appsettings.{Environment}.json", _environment);
                    }

                    foreach (var file in files) 
                        await CheckForChangesAsync(file, configIndexes, stoppingToken);

                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Consul yapılandırma izleme hatası, 5 saniye sonra yeniden denenecek");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Consul gerçek zamanlı yapılandırma izleyicisi beklenmeyen hata ile durdu");
        }
    }

    private async Task CheckForChangesAsync(string configFileName, Dictionary<string, ulong> configIndexes,
        CancellationToken stoppingToken)
    {
        var consulKey = $"{_serviceName}/config/{configFileName}";
        _logger.LogDebug("Checking Consul key: {ConsulKey}", consulKey);

        try
        {
            configIndexes.TryGetValue(configFileName, out ulong lastIndex);

            var queryOptions = new QueryOptions { WaitIndex = lastIndex };
            var response = await _consulClient.KV.Get(consulKey, queryOptions, stoppingToken);

            if (response?.Response == null)
            {
                _logger.LogWarning("Consul'da yapılandırma bulunamadı: {ConfigFile}", configFileName);
                return;
            }

            if (response.LastIndex > lastIndex)
            {
                configIndexes[configFileName] = response.LastIndex;

                var jsonContent = Encoding.UTF8.GetString(response.Response.Value);
                _logger.LogInformation("Consul'da yapılandırma değişikliği tespit edildi: {ConfigFile}",
                    configFileName);

                await _configProvider.LoadConfigFileAsync(configFileName, jsonContent);

                _configProvider.TriggerReload();

                _logger.LogInformation("Yapılandırma gerçek zamanlı olarak güncellendi: {ConfigFile}", configFileName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yapılandırma dosyası kontrolü sırasında hata: {ConfigFile}", configFileName);
        }
    }
}