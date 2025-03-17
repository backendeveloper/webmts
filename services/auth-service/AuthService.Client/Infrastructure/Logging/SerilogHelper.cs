using Serilog;
using Serilog.Sinks.Elasticsearch;

namespace AuthService.Client.Infrastructure.Logging;

public static class SerilogHelper
{
    public static void ConfigureLogging(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var elasticsearchUrl = configuration["Elasticsearch:Url"] ?? "http://elasticsearch:9200";
        
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Environment", environment)
            .Enrich.WithProperty("Application", "WebMTS.AuthService")
            // .Enrich.WithExceptionDetails() TODO: burasi yok
            .WriteTo.Console()
            .WriteTo.Debug()
            .WriteTo.Elasticsearch(ConfigureElasticSink(configuration, elasticsearchUrl, environment))
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }

    private static ElasticsearchSinkOptions ConfigureElasticSink(IConfiguration configuration, string elasticsearchUrl, string environment)
    {
        var indexFormat = string.Format(configuration["Elasticsearch:IndexFormat"] ?? "webmts-auth-{0:yyyy.MM}", DateTime.UtcNow);
        
        return new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
        {
            AutoRegisterTemplate = true,
            IndexFormat = indexFormat,
            NumberOfReplicas = 1,
            NumberOfShards = 2,
            ModifyConnectionSettings = x => x.BasicAuthentication("elastic", "changeme")
                .ServerCertificateValidationCallback((o, c, ch, e) => true)
        };
    }
}