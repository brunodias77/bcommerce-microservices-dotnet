using ClientService.Domain.Common;
using ClientService.Domain.Enums;
using ClientService.Domain.Validations;

namespace ClientService.Domain.Entities;

public class Consent : Entity
{
    public Guid ClientId { get; private set; }
    public ConsentType Type { get; private set; }
    public string? TermsVersion { get; private set; }
    public bool IsGranted { get; private set; }
    public int Version { get; private set; }

    private Consent() { } // EF Constructor

    private Consent(Guid clientId, ConsentType type, bool isGranted, string? termsVersion = null)
    {
        ClientId = clientId;
        Type = type;
        IsGranted = isGranted;
        TermsVersion = termsVersion;
        Version = 1;
    }

    public static Consent Create(Guid clientId, ConsentType type, bool isGranted, string? termsVersion = null)
    {
        return new Consent(clientId, type, isGranted, termsVersion);
    }

    public override void Validate(IValidationHandler handler)
    {
        throw new NotImplementedException();
    }
}