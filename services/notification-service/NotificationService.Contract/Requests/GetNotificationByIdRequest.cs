using System.ComponentModel.DataAnnotations;
using MediatR;
using NotificationService.Contract.Responses;

namespace NotificationService.Contract.Requests;

public class GetNotificationByIdRequest : IRequest<GetNotificationByIdResponse>
{
    [Required]
    public Guid NotificationId { get; set; }
}