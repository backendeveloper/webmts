namespace NotificationService.Contract.Dtos;

public class NotificationTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string SubjectTemplate { get; set; }
    public string BodyTemplate { get; set; }
    public bool IsActive { get; set; }
}