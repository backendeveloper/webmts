namespace CustomerService.Business.Events;

public interface IEventBus
{
    void Publish<T>(T @event) where T : IntegrationEvent;
}