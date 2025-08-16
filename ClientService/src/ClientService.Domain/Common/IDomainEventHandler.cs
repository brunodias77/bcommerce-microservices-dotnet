namespace ClientService.Domain.Common;

public interface IDomainEventHandler<in T> where T : DomainEvent
{
    Task Handle(T domainEvent, CancellationToken cancellationToken = default);
}