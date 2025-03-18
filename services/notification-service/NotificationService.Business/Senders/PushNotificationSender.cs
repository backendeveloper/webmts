using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationService.Business.Senders.Interfaces;
using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Senders;

public class PushNotificationSender : INotificationSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PushNotificationSender> _logger;

    public PushNotificationSender(IConfiguration configuration, ILogger<PushNotificationSender> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation($"Sending push notification to device {notification.RecipientInfo}");

            var pushApiKey = _configuration["PushSettings:ApiKey"];
            var pushAppId = _configuration["PushSettings:AppId"];

            await Task.Delay(300);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending push notification to device {notification.RecipientInfo}");
            return false;
        }
    }

    public bool CanHandle(NotificationType type)
    {
        return type == NotificationType.PushNotification;
    }
}