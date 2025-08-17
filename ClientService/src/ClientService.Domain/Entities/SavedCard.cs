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
        // Validação do ClientId
        if (ClientId == Guid.Empty)
            handler.Add(new Error("SavedCard.ClientId", "ClientId é obrigatório"));
    
        // Validação do Nickname (opcional)
        if (!string.IsNullOrEmpty(Nickname) && Nickname.Length > 100)
            handler.Add(new Error("SavedCard.Nickname", "Apelido deve ter no máximo 100 caracteres"));
    
        // Validação do LastFourDigits
        if (string.IsNullOrWhiteSpace(LastFourDigits))
            handler.Add(new Error("SavedCard.LastFourDigits", "Últimos quatro dígitos são obrigatórios"));
        else if (LastFourDigits.Length != 4)
            handler.Add(new Error("SavedCard.LastFourDigits", "Últimos quatro dígitos devem ter exatamente 4 caracteres"));
        else if (!LastFourDigits.All(char.IsDigit))
            handler.Add(new Error("SavedCard.LastFourDigits", "Últimos quatro dígitos devem conter apenas números"));
    
        // Validação do GatewayToken
        if (string.IsNullOrWhiteSpace(GatewayToken))
            handler.Add(new Error("SavedCard.GatewayToken", "Token do gateway é obrigatório"));
        else if (GatewayToken.Length > 255)
            handler.Add(new Error("SavedCard.GatewayToken", "Token do gateway deve ter no máximo 255 caracteres"));
    
        // Validação da ExpiryDate
        if (ExpiryDate <= DateTime.UtcNow.Date)
            handler.Add(new Error("SavedCard.ExpiryDate", "Data de vencimento deve ser futura"));
    }
}