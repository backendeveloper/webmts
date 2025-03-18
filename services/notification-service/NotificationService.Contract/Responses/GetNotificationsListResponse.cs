using NotificationService.Contract.Dtos;

namespace NotificationService.Contract.Responses;

public class GetNotificationsListResponse : BaseResponse
{
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public List<NotificationDto> Notifications { get; set; } = new List<NotificationDto>();
}