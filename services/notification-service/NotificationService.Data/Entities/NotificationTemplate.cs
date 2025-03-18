using System.ComponentModel.DataAnnotations;
using NotificationService.Contract.Enums;

namespace NotificationService.Data.Entities;

public class NotificationTemplate : BaseEntity
{
    [Key] public Guid Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public NotificationType Type { get; set; }

    public string SubjectTemplate { get; set; }

    public string BodyTemplate { get; set; }

    public bool IsActive { get; set; }
}