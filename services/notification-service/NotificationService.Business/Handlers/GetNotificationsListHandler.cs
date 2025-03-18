using MediatR;
using Microsoft.Extensions.Logging;
using NotificationService.Contract.Dtos;
using NotificationService.Contract.Requests;
using NotificationService.Contract.Responses;
using NotificationService.Data;

namespace NotificationService.Business.Handlers;

public class GetNotificationsListHandler : IRequestHandler<GetNotificationsListRequest, GetNotificationsListResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetNotificationsListHandler> _logger;

    public GetNotificationsListHandler(
        IUnitOfWork unitOfWork,
        ILogger<GetNotificationsListHandler> logger)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetNotificationsListResponse> Handle(GetNotificationsListRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting notifications list");

            var allNotifications = await _unitOfWork.Notifications.GetAllAsync();
            var totalCount = allNotifications.Count();

            var paginatedNotifications = allNotifications
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            var notificationDtos = paginatedNotifications.Select(paginatedNotification => new NotificationDto
                {
                    Id = paginatedNotification.Id,
                    Type = paginatedNotification.Type,
                    Subject = paginatedNotification.Subject,
                    Content = paginatedNotification.Content,
                    CreatedAt = paginatedNotification.CreatedAt,
                    RelatedEntityId = paginatedNotification.RelatedEntityId,
                    RecipientInfo = paginatedNotification.RecipientInfo,
                    RelatedEntityType = paginatedNotification.RelatedEntityType,
                    SentAt = paginatedNotification.SentAt,
                    RecipientId = paginatedNotification.RecipientId,
                    Status = paginatedNotification.Status
                })
                .ToList();

            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            return new GetNotificationsListResponse
            {
                Success = true,
                Message = "Notifications retrieved successfully",
                TotalCount = totalCount,
                PageSize = request.PageSize,
                PageNumber = request.PageNumber,
                TotalPages = totalPages,
                Notifications = notificationDtos
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notifications list");

            return new GetNotificationsListResponse
            {
                Success = false,
                Message = $"Failed to retrieve notifications: {ex.Message}"
            };
        }
    }
}