namespace CustomerService.Business.Events;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "webmts_events";
    public string QueueName { get; set; } = "customer_events";
}