using Consul;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TransactionService.Common.Configuration;

public class ConsulConfigurationMonitor : BackgroundService
{
    private readonly IConsulClient _consulClient;
    private readonly string _serviceName;
    private readonly string _environment;
    private readonly ILogger<ConsulConfigurationMonitor> _logger;
    private readonly IConfiguration _configuration;
    private readonly Dictionary<string, ulong> _lastIndices = new();

    public ConsulConfigurationMonitor(
        IConsulClient consulClient,
        IConfiguration configuration,
        ILogger<ConsulConfigurationMonitor> logger,
        IHostEnvironment hostEnvironment)
    {
        _consulClient = consulClient;
        _configuration = configuration;
        _logger = logger;
        _environment = hostEnvironment.EnvironmentName;

        _serviceName = configuration["Service:Name"] ?? "transaction-service";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Consul yapılandırma izleyicisi başlatılıyor...");

        var configFiles = new List<string> { "appsettings.json" };

        if (!string.IsNullOrEmpty(_environment))
        {
            configFiles.Add($"appsettings.{_environment}.json");
            _logger.LogInformation("Ortam yapılandırması izlenecek: {Environment}", _environment);
        }

        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var configFile in configFiles) 
                    await CheckConfigurationFileAsync(configFile, stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
               
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Consul yapılandırma izlemesi sırasında hata oluştu, 5 saniye sonra yeniden denenecek");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Consul yapılandırma izleyicisi durduruldu");
    }

    private async Task CheckConfigurationFileAsync(string configFile, CancellationToken stoppingToken)
    {
        var consulKey = $"{_serviceName}/config/{configFile}";

        try
        {
            _lastIndices.TryGetValue(configFile, out var lastIndex);

            var queryOptions = new QueryOptions { WaitIndex = lastIndex };
            var response = await _consulClient.KV.Get(consulKey, queryOptions, stoppingToken);

            if (response?.Response == null)
            {
                _logger.LogDebug("Consul'da yapılandırma bulunamadı: {ConfigFile}", configFile);
                return;
            }

            if (response.LastIndex > lastIndex)
            {
                _lastIndices[configFile] = response.LastIndex;
                _logger.LogInformation("Consul'da yapılandırma değişikliği tespit edildi: {ConfigFile}", configFile);

                if (_configuration is IConfigurationRoot configRoot)
                {
                    configRoot.Reload();
                    _logger.LogInformation("Yapılandırma başarıyla yeniden yüklendi: {ConfigFile}", configFile);

                    var testValue = _configuration["TestConfig:Value"];
                    _logger.LogInformation("Test yapılandırma değeri: {TestValue}", testValue);
                }
                else
                    _logger.LogWarning("Yapılandırma yeniden yüklenemedi - IConfigurationRoot tipinde değil");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Yapılandırma dosyası kontrolü sırasında hata: {ConfigFile}", configFile);
        }
    }
}