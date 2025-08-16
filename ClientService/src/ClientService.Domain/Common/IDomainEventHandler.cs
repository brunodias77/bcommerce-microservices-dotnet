namespace ClientService.Domain.Common;

public interface IDomainEventHandler<TDomainEvent> where TDomainEvent : DomainEvent
{

    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}