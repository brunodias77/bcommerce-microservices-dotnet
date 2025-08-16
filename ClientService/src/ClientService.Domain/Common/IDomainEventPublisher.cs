namespace ClientService.Domain.Common;

public interface IDomainEventPublisher
{

    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default)
        where T : DomainEvent;

    Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default);

}