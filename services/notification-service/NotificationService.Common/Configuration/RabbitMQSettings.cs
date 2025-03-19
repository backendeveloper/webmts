using System.Text.Json.Serialization;

namespace NotificationService.Common.Configuration;

public class RabbitMQSettings
{
    public string HostName { get; set; }
    public int Port { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public string VirtualHost { get; set; }
    public string ExchangeName { get; set; }
    public string QueueName { get; set; }
    
    [JsonIgnore]
    public string ConnectionString => $"amqp://{UserName}:{Password}@{HostName}:{Port}/{VirtualHost}";
}