using NotificationService.Contract.Dtos;

namespace NotificationService.Contract.Responses;

public class SendNotificationResponse : BaseResponse
{
    public NotificationDto Notification { get; set; }
}