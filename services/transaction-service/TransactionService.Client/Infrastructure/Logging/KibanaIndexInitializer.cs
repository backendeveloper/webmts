using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TransactionService.Client.Infrastructure.Logging;

public class KibanaIndexInitializer : BackgroundService
{
    private readonly ILogger<KibanaIndexInitializer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _kibanaUrl;
    private readonly string _indexPattern;
    private readonly string _username;
    private readonly string _password;
    
    public KibanaIndexInitializer(
        ILogger<KibanaIndexInitializer> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _kibanaUrl = configuration["Elasticsearch:KibanaUrl"] ?? "http://kibana:5601";
        _indexPattern = configuration["Elasticsearch:IndexPattern"] ?? "webmts-transaction-*";
        _username = configuration["Elasticsearch:Username"] ?? "elastic";
        _password = configuration["Elasticsearch:Password"] ?? "changeme";
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kibana başlatılıyor... (60 saniye bekleniyor)");
        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        
        try
        {
            if (await IsKibanaReadyAsync(stoppingToken))
                await CreateIndexPatternAsync(stoppingToken);
            else
                _logger.LogWarning("Kibana hazır değil, indeks pattern oluşturulamadı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kibana indeks pattern oluşturma hatası");
        }
    }
    
    private async Task<bool> IsKibanaReadyAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();
        
        try
        {
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    var response = await client.GetAsync($"{_kibanaUrl}/api/status", cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Kibana API hazır");
                        return true;
                    }
                }
                catch
                {
                }

                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kibana hazırlık kontrolü başarısız");
            return false;
        }
    }
    
    private async Task CreateIndexPatternAsync(CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();
        
        if (!string.IsNullOrEmpty(_username) && !string.IsNullOrEmpty(_password))
        {
            var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authValue);
        }
        
        try
        {
            var patternId = _indexPattern.Replace("*", "").ToLowerInvariant();
            var checkResponse = await client.GetAsync(
                $"{_kibanaUrl}/api/saved_objects/index-pattern/{patternId}", 
                cancellationToken);
            
            if (checkResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("İndeks pattern zaten mevcut: {Pattern}", _indexPattern);
                return;
            }
            
            var patternData = new
            {
                attributes = new
                {
                    title = _indexPattern,
                    timeFieldName = "@timestamp"
                }
            };
            
            var content = new StringContent(
                JsonSerializer.Serialize(patternData),
                Encoding.UTF8,
                "application/json");
            
            var createResponse = await client.PostAsync(
                $"{_kibanaUrl}/api/saved_objects/index-pattern/{patternId}",
                content,
                cancellationToken);
            
            if (createResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("İndeks pattern başarıyla oluşturuldu: {Pattern}", _indexPattern);
                
                var defaultData = new { value = patternId };
                var defaultContent = new StringContent(
                    JsonSerializer.Serialize(defaultData),
                    Encoding.UTF8,
                    "application/json");
                
                var defaultResponse = await client.PostAsync(
                    $"{_kibanaUrl}/api/kibana/settings/defaultIndex",
                    defaultContent,
                    cancellationToken);
                
                if (defaultResponse.IsSuccessStatusCode)
                    _logger.LogInformation("Varsayılan indeks pattern olarak ayarlandı: {Pattern}", _indexPattern);
                else
                    _logger.LogWarning("Varsayılan indeks ayarlanamadı: {Status}", defaultResponse.StatusCode);
            }
            else
            {
                string errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("İndeks pattern oluşturulamadı: {Status} - {Error}", 
                    createResponse.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İndeks pattern API hatası");
        }
    }
}