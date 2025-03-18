using Microsoft.Extensions.Logging;
using NotificationService.Business.Senders.Interfaces;
using NotificationService.Contract.Enums;

namespace NotificationService.Business.Senders;

public class NotificationSenderFactory
{
    private readonly IEnumerable<INotificationSender> _senders;
    private readonly ILogger<NotificationSenderFactory> _logger;

    public NotificationSenderFactory(IEnumerable<INotificationSender> senders, ILogger<NotificationSenderFactory> logger)
    {
        _senders = senders ?? throw new ArgumentNullException(nameof(senders));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public INotificationSender GetSender(NotificationType type)
    {
        var sender = _senders.FirstOrDefault(s => s.CanHandle(type));
        if (sender == null)
        {
            _logger.LogWarning($"No sender found for notification type {type}");
            throw new InvalidOperationException($"No sender found for notification type {type}");
        }
            
        return sender;
    }
}