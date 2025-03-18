using MediatR;
using NotificationService.Contract.Enums;
using NotificationService.Contract.Responses;

namespace NotificationService.Contract.Requests;

public class GetNotificationsListRequest : IRequest<GetNotificationsListResponse>
{
    public NotificationType? Type { get; set; }
    public NotificationStatusType? Status { get; set; }
    public string RecipientId { get; set; }
    public string RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageSize { get; set; } = 10;
    public int PageNumber { get; set; } = 1;
}