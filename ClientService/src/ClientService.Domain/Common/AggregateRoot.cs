using System.Collections.ObjectModel;

namespace ClientService.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<DomainEvent> _events = new();
    public IReadOnlyCollection<DomainEvent> Events => new ReadOnlyCollection<DomainEvent>(_events);
    protected AggregateRoot() : base() { }
    public void RaiseEvent(DomainEvent @event) => _events.Add(@event);
    public void AddDomainEvent(DomainEvent @event) => RaiseEvent(@event);
    public void ClearEvents() => _events.Clear();
    public bool HasEvents => _events.Count > 0;
}