namespace Domain.Shared;

public interface IEventHandler<T> where T : IDomainEvent
{
    void Handle(T domainEvent);
}
