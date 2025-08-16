using ClientService.Domain.Common;
using ClientService.Domain.Enums;
using ClientService.Domain.Validations;
using ClientService.Domain.ValueObjects;

namespace ClientService.Domain.Entities;

public class SavedCard : Entity
{
    public Guid ClientId { get; private set; }
    public string? Nickname { get; private set; }
    public string LastFourDigits { get; private set; } // Mudar de CardNumber para LastFourDigits
    public CardBrand Brand { get; private set; }
    public string GatewayToken { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public bool IsDefault { get; private set; }
    public int Version { get; private set; }

    private SavedCard() { } // EF Constructor

    private SavedCard(Guid clientId, string? nickname, string lastFourDigits,
        CardBrand brand, string gatewayToken, DateTime expiryDate,
        bool isDefault = false)
    {
        Id = Guid.NewGuid();
        ClientId = clientId;
        Nickname = nickname;
        LastFourDigits = lastFourDigits;
        Brand = brand;
        GatewayToken = gatewayToken;
        ExpiryDate = expiryDate;
        IsDefault = isDefault;
        Version = 1;
    }

    public static SavedCard Create(Guid clientId, string? nickname, string lastFourDigits,
        CardBrand brand, string gatewayToken, DateTime expiryDate,
        bool isDefault = false)
    {
        return new SavedCard(clientId, nickname, lastFourDigits, brand, gatewayToken,
            expiryDate, isDefault);
    }
    
    public override void Validate(IValidationHandler handler)
    {
        throw new NotImplementedException();
    }
}