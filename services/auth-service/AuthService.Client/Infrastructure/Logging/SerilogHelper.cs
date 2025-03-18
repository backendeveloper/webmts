using System.Text.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Elasticsearch;

namespace AuthService.Client.Infrastructure.Logging;

public static class SerilogHelper
{
    public static void ConfigureLogging(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var serviceName = "webmts-auth";

        // Elastic konfigürasyonu
        var elasticsearchUrl = configuration["Elasticsearch:Url"] ?? "http://elasticsearch:9200";
        var elasticsearchUsername = configuration["Elasticsearch:Username"] ?? "elastic";
        var elasticsearchPassword = configuration["Elasticsearch:Password"] ?? "changeme";

        // İndeks formatı: webmts-auth-YYYY.MM - Bu Kibana'nın aradığı formata uygun
        var indexFormat = $"{serviceName}-{DateTime.UtcNow:yyyy.MM}";

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", serviceName)
            .Enrich.WithProperty("Environment", environment)
            .WriteTo.Console()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUrl))
            {
                IndexFormat = indexFormat,
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                ModifyConnectionSettings = conn =>
                {
                    conn.BasicAuthentication(elasticsearchUsername, elasticsearchPassword);
                    conn.ServerCertificateValidationCallback((o, c, ch, e) => true);
                    return conn;
                },
                EmitEventFailure = EmitEventFailureHandling.WriteToSelfLog |
                                   EmitEventFailureHandling.WriteToFailureSink,

                // Bu özelliklerden kurtularak hataları önlüyoruz
                // FailureCallback = ..., 
                // CustomFormatter = ...,

                // Index ayarları
                NumberOfReplicas = 1,
                NumberOfShards = 2
            });

        Log.Logger = loggerConfig.CreateLogger();
    }
}

public class EsCustomFormatter : ITextFormatter
{
    public void Format(LogEvent logEvent, TextWriter output)
    {
        var formattedData = new Dictionary<string, object>
        {
            ["@timestamp"] = logEvent.Timestamp.UtcDateTime,
            ["level"] = logEvent.Level.ToString(),
            ["message"] = logEvent.RenderMessage(),
            ["exception"] = logEvent.Exception?.ToString() ?? string.Empty
        };

        // Add all properties from the log event
        foreach (var property in logEvent.Properties)
        {
            // Convert the property value to a simpler form
            formattedData[property.Key] = ConvertPropertyValue(property.Value);
        }

        // Serialize to JSON and write to the output
        var json = JsonSerializer.Serialize(formattedData);
        output.Write(json);
    }

    private static object ConvertPropertyValue(LogEventPropertyValue propertyValue)
    {
        // Handle different types of property values
        return propertyValue switch
        {
            ScalarValue scalarValue => scalarValue.Value ?? "",
            SequenceValue sequenceValue => sequenceValue.Elements.Select(ConvertPropertyValue).ToArray(),
            StructureValue structureValue => structureValue.Properties.ToDictionary(p => p.Name,
                p => ConvertPropertyValue(p.Value)),
            DictionaryValue dictionaryValue => dictionaryValue.Elements.ToDictionary(
                kvp => kvp.Key.Value?.ToString() ?? "",
                kvp => ConvertPropertyValue(kvp.Value)),
            _ => propertyValue.ToString() ?? ""
        };
    }
}