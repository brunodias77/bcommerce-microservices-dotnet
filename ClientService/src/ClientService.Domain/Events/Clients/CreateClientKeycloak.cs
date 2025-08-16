using ClientService.Domain.Common;
using ClientService.Domain.Enums;

namespace ClientService.Domain.Events.Clients;

public record CreateClientKeycloak(
    string KeycloakUserId,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    string? Phone,
    DateTime? DateOfBirth,
    string? Cpf,
    bool NewsletterOptIn
) : DomainEvent;