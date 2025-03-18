using Microsoft.Extensions.Logging;
using NotificationService.Business.Senders.Interfaces;
using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Senders;

public class SystemNotificationSender : INotificationSender
{
    private readonly ILogger<SystemNotificationSender> _logger;

    public SystemNotificationSender(ILogger<SystemNotificationSender> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(Notification notification)
    {
        try
        {
            _logger.LogInformation($"Sending system notification to user {notification.RecipientId}");
            await Task.Delay(200);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending system notification to user {notification.RecipientId}");
            return false;
        }
    }

    public bool CanHandle(NotificationType type)
    {
        return type == NotificationType.System;
    }
}