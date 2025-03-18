using System.ComponentModel.DataAnnotations;
using NotificationService.Contract.Enums;

namespace NotificationService.Data.Entities;

public class Notification : BaseEntity
{
    [Key]
    public Guid Id { get; set; }
        
    public NotificationType Type { get; set; }
        
    public string Subject { get; set; }
        
    public string Content { get; set; }
        
    public string TemplateId { get; set; }
        
    public string RecipientId { get; set; }
        
    public string RecipientInfo { get; set; }
        
    public NotificationStatusType Status { get; set; }
        
    public DateTime? SentAt { get; set; }
        
    public DateTime? LastAttemptAt { get; set; }
        
    public int RetryCount { get; set; }
        
    public string ErrorMessage { get; set; }
        
    public string RelatedEntityId { get; set; }
        
    public string RelatedEntityType { get; set; }
}