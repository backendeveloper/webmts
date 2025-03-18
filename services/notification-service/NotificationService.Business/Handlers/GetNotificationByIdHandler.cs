using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Contract.Dtos;
using NotificationService.Contract.Requests;
using NotificationService.Contract.Responses;
using NotificationService.Data;

namespace NotificationService.Business.Handlers;

public class GetNotificationByIdHandler : IRequestHandler<GetNotificationByIdRequest, GetNotificationByIdResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetNotificationByIdHandler> _logger;

    public GetNotificationByIdHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetNotificationByIdHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetNotificationByIdResponse> Handle(GetNotificationByIdRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation($"Getting notification details for notification {request.NotificationId}");

            var notification = await _unitOfWork.Notifications.GetByIdAsync(request.NotificationId);
            if (notification == null)
                return new GetNotificationByIdResponse
                {
                    Success = false,
                    Message = $"Notification with id {request.NotificationId} not found"
                };

            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                Type = notification.Type,
                Subject = notification.Subject,
                Content = notification.Content,
                CreatedAt = notification.CreatedAt,
                RelatedEntityId = notification.RelatedEntityId,
                RecipientInfo = notification.RecipientInfo,
                RelatedEntityType = notification.RelatedEntityType,
                SentAt = notification.SentAt,
                RecipientId = notification.RecipientId,
                Status = notification.Status
            };

            return new GetNotificationByIdResponse
            {
                Success = true,
                Message = "Notification retrieved successfully",
                Notification = notificationDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving notification {request.NotificationId}");

            return new GetNotificationByIdResponse
            {
                Success = false,
                Message = $"Failed to retrieve notification: {ex.Message}"
            };
        }
    }
}