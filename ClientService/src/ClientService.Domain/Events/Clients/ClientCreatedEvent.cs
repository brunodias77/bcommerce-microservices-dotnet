using ClientService.Domain.Common;

namespace ClientService.Domain.Events.Clients;

public record ClientCreatedEvent(
    Guid ClientId,
    string Email,
    string FirstName,
    string LastName
) : DomainEvent;