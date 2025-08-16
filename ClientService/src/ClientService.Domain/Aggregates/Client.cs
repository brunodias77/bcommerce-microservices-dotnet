using ClientService.Domain.Common;
using ClientService.Domain.Entities;
using ClientService.Domain.Enums;
using ClientService.Domain.Validations;
using ClientService.Domain.ValueObjects;

namespace ClientService.Domain.Aggregates;

public class Client : AggregateRoot
{
    public Guid? KeycloakUserId { get; private set; } 
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Cpf? Cpf { get; private set; }
    public DateTime? DateOfBirth { get; private set; }
    public Phone? Phone { get; private set; }
    public bool NewsletterOptIn { get; private set; }
    public ClientStatus Status { get; private set; }
    public UserRole Role { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTime? AccountLockedUntil { get; private set; }
    public DateTime? EmailVerifiedAt { get; private set; }
    public int Version { get; private set; }

    private readonly List<Address> _addresses = new();
    private readonly List<Consent> _consents = new();
    private readonly List<SavedCard> _savedCards = new();

    public IReadOnlyCollection<Address> Addresses => _addresses.AsReadOnly();
    public IReadOnlyCollection<Consent> Consents => _consents.AsReadOnly();
    public IReadOnlyCollection<SavedCard> SavedCards => _savedCards.AsReadOnly();

    private Client() { } // EF Core
    
    public override void Validate(IValidationHandler handler)
    {
        throw new NotImplementedException();
    }
}