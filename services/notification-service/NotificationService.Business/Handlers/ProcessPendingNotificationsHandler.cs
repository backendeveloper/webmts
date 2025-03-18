using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Business.Renderers;
using NotificationService.Contract.Enums;
using NotificationService.Contract.Requests;
using NotificationService.Data;

namespace NotificationService.Business.Handlers;

public class ProcessPendingNotificationsHandler : IRequestHandler<ProcessPendingNotificationsRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPendingNotificationsHandler> _logger;
    private readonly Services.NotificationService _notificationService;
    private readonly ITemplateRenderer _templateRenderer;

    public ProcessPendingNotificationsHandler(
        IUnitOfWork unitOfWork,
        ILogger<ProcessPendingNotificationsHandler> logger,
        Services.NotificationService notificationService,
        ITemplateRenderer templateRenderer)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
    }

    public async Task<bool> Handle(ProcessPendingNotificationsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Processing pending notifications, batch size: {request.BatchSize}");

            var pendingNotifications =
                await _unitOfWork.Notifications.GetPendingNotificationsAsync(request.BatchSize);
            var notificationsList = pendingNotifications.ToList();
            if (notificationsList.Count == 0)
            {
                _logger.LogInformation("No pending notifications found");
                return true;
            }

            _logger.LogInformation($"Found {notificationsList.Count} pending notifications");

            foreach (var notification in notificationsList)
            {
                try
                {
                    var result = await _notificationService.SendNotificationAsync(notification);
                    if (result)
                    {
                        notification.Status = NotificationStatusType.Sent;
                        notification.SentAt = DateTime.UtcNow;
                        _logger.LogInformation($"Notification {notification.Id} sent successfully");
                    }
                    else
                    {
                        notification.LastAttemptAt = DateTime.UtcNow;
                        notification.RetryCount++;
                        if (notification.RetryCount >= 3)
                        {
                            notification.Status = NotificationStatusType.Failed;
                            notification.ErrorMessage = "Max retry count exceeded";
                            _logger.LogWarning(
                                $"Notification {notification.Id} failed after {notification.RetryCount} attempts");
                        }
                        else
                        {
                            _logger.LogWarning(
                                $"Notification {notification.Id} sending failed, retry count: {notification.RetryCount}");
                        }
                    }

                    _unitOfWork.Notifications.Update(notification);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing notification {notification.Id}");

                    notification.LastAttemptAt = DateTime.UtcNow;
                    notification.RetryCount++;
                    notification.ErrorMessage = $"Error: {ex.Message}";
                    if (notification.RetryCount >= 3)
                        notification.Status = NotificationStatusType.Failed;

                    _unitOfWork.Notifications.Update(notification);
                }
            }

            await _unitOfWork.CompleteAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending notifications batch");
            return false;
        }
    }
}