using NotificationService.Contract.Enums;

namespace NotificationService.Contract.Dtos;

public class NotificationDto
{
    public Guid Id { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; }
    public string Content { get; set; }
    public string RecipientId { get; set; }
    public string RecipientInfo { get; set; }
    public NotificationStatusType Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; }
}