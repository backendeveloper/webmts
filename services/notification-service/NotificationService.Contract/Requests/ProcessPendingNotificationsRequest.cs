using MediatR;

namespace NotificationService.Contract.Requests;

public class ProcessPendingNotificationsRequest : IRequest<bool>
{
    public int BatchSize { get; set; } = 10;
}