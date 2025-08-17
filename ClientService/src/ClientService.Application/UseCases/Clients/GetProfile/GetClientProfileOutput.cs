using ClientService.Domain.Enums;

namespace ClientService.Application.UseCases.Clients.GetProfile;

public record GetClientProfileOutput(
    Guid Id,
    Guid? KeycloakUserId,
    string FirstName,
    string LastName,
    string Email,
    string? Cpf,
    DateTime? DateOfBirth,
    string? Phone,
    bool NewsletterOptIn,
    string Status,
    string Role,
    int FailedLoginAttempts,
    DateTime? AccountLockedUntil,
    DateTime? EmailVerifiedAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IEnumerable<AddressDto> Addresses,
    IEnumerable<ConsentDto> Consents,
    IEnumerable<SavedCardDto> SavedCards,
    DateTime Timestamp
);

public record AddressDto(
    Guid Id,
    string Street,
    string Number,
    string? Complement,
    string Neighborhood,
    string City,
    string State,
    string ZipCode,
    string Country,
    bool IsDefault,
    string Type
);

public record ConsentDto(
    Guid Id,
    string Type,
    bool IsGranted,
    DateTime GrantedAt,
    DateTime? RevokedAt,
    string? IpAddress,
    string? UserAgent
);

public record SavedCardDto(
    Guid Id,
    string Nickname,
    string Brand,
    string LastFourDigits,
    DateTime ExpiryDate,
    bool IsDefault
);