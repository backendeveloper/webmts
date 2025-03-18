using NotificationService.Contract.Dtos;

namespace NotificationService.Contract.Responses;

public class GetNotificationByIdResponse : BaseResponse
{
    public NotificationDto Notification { get; set; }
}