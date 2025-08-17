using ClientService.Domain.Common;
using ClientService.Domain.Enums;
using ClientService.Domain.Validations;

namespace ClientService.Domain.Entities;

public class Address : Entity
{
    public Guid ClientId { get; private set; }
    public AddressType Type { get; private set; }
    // public string RecipientName { get; private set; } // REMOVER
    public string PostalCode { get; private set; }
    public string Street { get; private set; }
    public string StreetNumber { get; private set; }
    public string? Complement { get; private set; }
    public string Neighborhood { get; private set; }
    public string City { get; private set; }
    public string StateCode { get; private set; }
    public string CountryCode { get; private set; }
    public string? Phone { get; private set; }
    public bool IsDefault { get; private set; }
    public int Version { get; private set; }

    private Address() { } // EF Constructor

    private Address(Guid clientId, AddressType type, 
                   string postalCode, string street, string streetNumber,
                   string? complement, string neighborhood, string city, 
                   string stateCode, string countryCode, string? phone, 
                   bool isDefault = false)
    {
        ClientId = clientId;
        Type = type;
        PostalCode = postalCode;
        Street = street;
        StreetNumber = streetNumber;
        Complement = complement;
        Neighborhood = neighborhood;
        City = city;
        StateCode = stateCode;
        CountryCode = countryCode;
        Phone = phone;
        IsDefault = isDefault;
        Version = 1;
    }

    public static Address Create(Guid clientId, AddressType type,
                               string postalCode, string street, string streetNumber,
                               string? complement, string neighborhood, string city,
                               string stateCode, string countryCode = "BR", 
                               string? phone = null, bool isDefault = false)
    {
        return new Address(clientId, type, postalCode, street, 
                         streetNumber, complement, neighborhood, city, stateCode, 
                         countryCode, phone, isDefault);
    }

    public override void Validate(IValidationHandler handler)
    {
        // Validação do ClientId
        if (ClientId == Guid.Empty)
            handler.Add(new Error("Address.ClientId", "ClientId é obrigatório"));
    
        // Validação do PostalCode
        if (string.IsNullOrWhiteSpace(PostalCode))
            handler.Add(new Error("Address.PostalCode", "CEP é obrigatório"));
        else if (PostalCode.Length < 8 || PostalCode.Length > 9)
            handler.Add(new Error("Address.PostalCode", "CEP deve ter entre 8 e 9 caracteres"));
    
        // Validação da Street
        if (string.IsNullOrWhiteSpace(Street))
            handler.Add(new Error("Address.Street", "Logradouro é obrigatório"));
        else if (Street.Length > 200)
            handler.Add(new Error("Address.Street", "Logradouro deve ter no máximo 200 caracteres"));
    
        // Validação do StreetNumber
        if (string.IsNullOrWhiteSpace(StreetNumber))
            handler.Add(new Error("Address.StreetNumber", "Número é obrigatório"));
        else if (StreetNumber.Length > 20)
            handler.Add(new Error("Address.StreetNumber", "Número deve ter no máximo 20 caracteres"));
    
        // Validação do Complement (opcional)
        if (!string.IsNullOrEmpty(Complement) && Complement.Length > 100)
            handler.Add(new Error("Address.Complement", "Complemento deve ter no máximo 100 caracteres"));
    
        // Validação do Neighborhood
        if (string.IsNullOrWhiteSpace(Neighborhood))
            handler.Add(new Error("Address.Neighborhood", "Bairro é obrigatório"));
        else if (Neighborhood.Length > 100)
            handler.Add(new Error("Address.Neighborhood", "Bairro deve ter no máximo 100 caracteres"));
    
        // Validação da City
        if (string.IsNullOrWhiteSpace(City))
            handler.Add(new Error("Address.City", "Cidade é obrigatória"));
        else if (City.Length > 100)
            handler.Add(new Error("Address.City", "Cidade deve ter no máximo 100 caracteres"));
    
        // Validação do StateCode
        if (string.IsNullOrWhiteSpace(StateCode))
            handler.Add(new Error("Address.StateCode", "Código do estado é obrigatório"));
        else if (StateCode.Length != 2)
            handler.Add(new Error("Address.StateCode", "Código do estado deve ter 2 caracteres"));
    
        // Validação do CountryCode
        if (string.IsNullOrWhiteSpace(CountryCode))
            handler.Add(new Error("Address.CountryCode", "Código do país é obrigatório"));
        else if (CountryCode.Length != 2)
            handler.Add(new Error("Address.CountryCode", "Código do país deve ter 2 caracteres"));
    
        // Validação do Phone (opcional)
        if (!string.IsNullOrEmpty(Phone) && (Phone.Length < 10 || Phone.Length > 15))
            handler.Add(new Error("Address.Phone", "Telefone deve ter entre 10 e 15 caracteres"));
    }
}