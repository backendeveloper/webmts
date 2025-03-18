using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Business.Renderers;
using NotificationService.Contract.Dtos;
using NotificationService.Contract.Enums;
using NotificationService.Contract.Requests;
using NotificationService.Contract.Responses;
using NotificationService.Data;
using NotificationService.Data.Entities;

namespace NotificationService.Business.Handlers;

public class SendNotificationHandler : IRequestHandler<SendNotificationRequest, SendNotificationResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendNotificationHandler> _logger;
    private readonly Services.NotificationService _notificationService;
    private readonly ITemplateRenderer _templateRenderer;

    public SendNotificationHandler(
        IUnitOfWork unitOfWork,
        ILogger<SendNotificationHandler> logger,
        Services.NotificationService notificationService,
        ITemplateRenderer templateRenderer)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
    }

    public async Task<SendNotificationResponse> Handle(SendNotificationRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating a new notification");

        try
        {
            var subject = request.Subject;
            var content = request.Content;

            if (!string.IsNullOrEmpty(request.TemplateName) || !string.IsNullOrEmpty(request.TemplateId))
            {
                NotificationTemplate template = null;

                if (!string.IsNullOrEmpty(request.TemplateName))
                    template = await _unitOfWork.NotificationTemplates.GetTemplateByNameAsync(request.TemplateName);
                else if (!string.IsNullOrEmpty(request.TemplateId) &&
                         Guid.TryParse(request.TemplateId, out var templateId))
                    template = await _unitOfWork.NotificationTemplates.GetByIdAsync(templateId);

                if (template != null)
                {
                    subject = _templateRenderer.RenderTemplate(template.SubjectTemplate, request.TemplateData);
                    content = _templateRenderer.RenderTemplate(template.BodyTemplate, request.TemplateData);
                }
                else
                    return new SendNotificationResponse
                    {
                        Success = false,
                        Message = $"Template not found with name {request.TemplateName} or id {request.TemplateId}"
                    };
            }

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                Type = request.Type,
                Subject = subject,
                Content = content,
                TemplateId = request.TemplateId,
                RecipientId = request.RecipientId,
                RecipientInfo = request.RecipientInfo,
                Status = NotificationStatusType.Pending,
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0,
                RelatedEntityId = request.RelatedEntityId,
                RelatedEntityType = request.RelatedEntityType
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            var result = await _notificationService.SendNotificationAsync(notification);

            if (result)
            {
                notification.Status = NotificationStatusType.Sent;
                notification.SentAt = DateTime.UtcNow;
            }
            else
            {
                notification.Status = NotificationStatusType.Failed;
                notification.ErrorMessage = "Failed to send notification";
                notification.LastAttemptAt = DateTime.UtcNow;
                notification.RetryCount++;
            }

            _unitOfWork.Notifications.Update(notification);
            await _unitOfWork.CompleteAsync();

            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                SentAt = notification.SentAt,
                Status = notification.Status,
                Type = notification.Type,
                Subject = notification.Subject,
                Content = notification.Content,
                CreatedAt = notification.CreatedAt,
                RecipientInfo = notification.RecipientInfo,
                RecipientId = notification.RecipientId,
                RelatedEntityId = notification.RelatedEntityId,
                RelatedEntityType = notification.RelatedEntityType
            };

            return new SendNotificationResponse
            {
                Success = true,
                Message = result ? "Notification sent successfully" : "Notification created but sending failed",
                Notification = notificationDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/sending notification");

            return new SendNotificationResponse
            {
                Success = false,
                Message = $"Failed to create/send notification: {ex.Message}"
            };
        }
    }
}