namespace NotificationService.Contract.Events;

public abstract class IntegrationEvent
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected IntegrationEvent()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
}