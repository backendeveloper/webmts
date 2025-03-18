using NotificationService.Contract.Enums;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Senders.Interfaces;

public interface INotificationSender
{
    Task<bool> SendAsync(Notification notification);
    bool CanHandle(NotificationType type);
}