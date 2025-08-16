using ClientService.Domain.Enums;
using ClientService.Domain.ValueObjects;

namespace ClientService.Domain.Entities;

public class SavedCard
{
    public Guid ClientId { get; private set; }
    public string? Nickname { get; private set; }
    public CardNumber CardNumber { get; private set; }
    public CardBrand Brand { get; private set; }
    public string GatewayToken { get; private set; }
    public DateTime ExpiryDate { get; private set; }
    public bool IsDefault { get; private set; }
    public int Version { get; private set; }

    private SavedCard() { } // EF Constructor

    private SavedCard(Guid clientId, string? nickname, CardNumber cardNumber, 
        CardBrand brand, string gatewayToken, DateTime expiryDate, 
        bool isDefault = false)
    {
        ClientId = clientId;
        Nickname = nickname;
        CardNumber = cardNumber;
        Brand = brand;
        GatewayToken = gatewayToken;
        ExpiryDate = expiryDate;
        IsDefault = isDefault;
        Version = 1;
    }

    public static SavedCard Create(Guid clientId, string? nickname, string cardNumber,
        CardBrand brand, string gatewayToken, DateTime expiryDate,
        bool isDefault = false)
    {
        var cardNumberVO = CardNumber.Create(cardNumber);
        return new SavedCard(clientId, nickname, cardNumberVO, brand, gatewayToken, 
            expiryDate, isDefault);
    }
}