using Microsoft.Extensions.Logging;
using NotificationService.Business.Renderers;
using NotificationService.Business.Senders;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Services;

public class NotificationService
{
    private readonly NotificationSenderFactory _senderFactory;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationSenderFactory senderFactory,
        ITemplateRenderer templateRenderer,
        ILogger<NotificationService> logger)
    {
        _senderFactory = senderFactory ?? throw new ArgumentNullException(nameof(senderFactory));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendNotificationAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation($"Processing notification {notification.Id} of type {notification.Type}");

            var sender = _senderFactory.GetSender(notification.Type);
            var result = await sender.SendAsync(notification);

            _logger.LogInformation($"Notification {notification.Id} sent: {result}");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending notification {notification.Id}");
            return false;
        }
    }

    public string RenderTemplate(string template, Dictionary<string, string> data)
    {
        return _templateRenderer.RenderTemplate(template, data);
    }
}