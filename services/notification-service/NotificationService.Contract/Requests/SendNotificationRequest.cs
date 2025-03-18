using System.ComponentModel.DataAnnotations;
using MediatR;
using NotificationService.Contract.Enums;
using NotificationService.Contract.Responses;

namespace NotificationService.Contract.Requests;

public class SendNotificationRequest : IRequest<SendNotificationResponse>
{
    [Required]
    public NotificationType Type { get; set; }
        
    [Required]
    public string RecipientId { get; set; }
        
    [Required]
    public string RecipientInfo { get; set; }
        
    public string Subject { get; set; }
        
    public string Content { get; set; }
        
    public string TemplateId { get; set; }
        
    public string TemplateName { get; set; }
        
    public Dictionary<string, string> TemplateData { get; set; } = new Dictionary<string, string>();
        
    public string RelatedEntityId { get; set; }
        
    public string RelatedEntityType { get; set; }
}