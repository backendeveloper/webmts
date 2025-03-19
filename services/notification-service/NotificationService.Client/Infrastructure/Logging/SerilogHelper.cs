using System.Text.Json;
using Serilog;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.Elasticsearch;

namespace NotificationService.Client.Infrastructure.Logging;

public static class SerilogHelper
{
    public static void ConfigureLogging(IConfiguration configuration)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var serviceName = "webmts-notification";

        var elasticsearchUrl = configuration["Elasticsearch:Url"] ?? "http://elasticsearch:9200";
        var elasticsearchUsername = configuration["Elasticsearch:Username"] ?? "elastic";
        var elasticsearchPassword = configuration["Elasticsearch:Password"] ?? "changeme";

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

        foreach (var property in logEvent.Properties) 
            formattedData[property.Key] = ConvertPropertyValue(property.Value);

        var json = JsonSerializer.Serialize(formattedData);
        output.Write(json);
    }

    private static object ConvertPropertyValue(LogEventPropertyValue propertyValue)
    {
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