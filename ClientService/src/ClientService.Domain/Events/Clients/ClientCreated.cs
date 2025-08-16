using ClientService.Domain.Common;
using ClientService.Domain.Enums;

namespace ClientService.Domain.Events.Clients;

public record ClientCreated : DomainEvent
{
    public Guid ClientId { get; }
    public string Email { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public UserRole Role { get; }
    public string KeycloakUserId { get; }
    public string? Phone { get; }
    public DateTime? DateOfBirth { get; }
    public string? Cpf { get; }

    public ClientCreated(
        Guid clientId,
        string email,
        string firstName,
        string lastName,
        UserRole role,
        string keycloakUserId,
        string? phone = null,
        DateTime? dateOfBirth = null,
        string? cpf = null)
    {
        ClientId = clientId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
        KeycloakUserId = keycloakUserId;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        Cpf = cpf;
    }
}