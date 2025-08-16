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
        throw new NotImplementedException();
    }
}