using ClientService.Application.Common;
using ClientService.Domain.Common;
using ClientService.Domain.Services;
using ClientService.Domain.Validations;
using Microsoft.Extensions.Logging;

namespace ClientService.Application.UseCases.Clients.GetProfile;

public class GetClientProfileUseCase : IGetClientProfileUseCase
{
    private readonly ILoggedUser _loggedUser;
    private readonly ILogger<GetClientProfileUseCase> _logger;

    public GetClientProfileUseCase(
        ILoggedUser loggedUser,
        ILogger<GetClientProfileUseCase> logger)
    {
        _loggedUser = loggedUser;
        _logger = logger;
    }

    public async Task<Result<GetClientProfileOutput, Notification>> Execute()
    {
        var notification = new Notification();
        
        try
        {
            _logger.LogInformation("Iniciando busca do perfil do cliente logado");
            
            // Obter cliente logado
            var client = await _loggedUser.User();
            if (client == null)
            {
                notification.Add(new Error("client.notFound", "Cliente não encontrado ou não autenticado"));
                _logger.LogWarning("Cliente não encontrado ou não autenticado");
                return Result<GetClientProfileOutput, Notification>.Fail(notification);
            }

            // Mapear para output
            var output = new GetClientProfileOutput(
                Id: client.Id,
                KeycloakUserId: client.KeycloakUserId,
                FirstName: client.FirstName,
                LastName: client.LastName,
                Email: client.Email.Value,
                Cpf: client.Cpf?.Value,
                DateOfBirth: client.DateOfBirth,
                Phone: client.Phone?.Value,
                NewsletterOptIn: client.NewsletterOptIn,
                Status: client.Status.ToString(),
                Role: client.Role.ToString(),
                FailedLoginAttempts: client.FailedLoginAttempts,
                AccountLockedUntil: client.AccountLockedUntil,
                EmailVerifiedAt: client.EmailVerifiedAt,
                CreatedAt: client.CreatedAt,
                UpdatedAt: client.UpdatedAt,
                Addresses: client.Addresses.Select(a => new AddressDto(
                    Id: a.Id,
                    Street: a.Street,
                    Number: a.StreetNumber,
                    Complement: a.Complement,
                    Neighborhood: a.Neighborhood,
                    City: a.City,
                    State: a.StateCode,
                    ZipCode: a.PostalCode,
                    Country: a.CountryCode,
                    IsDefault: a.IsDefault,
                    Type: a.Type.ToString()
                )),
                Consents: client.Consents.Select(c => new ConsentDto(
                    Id: c.Id,
                    Type: c.Type.ToString(),
                    IsGranted: c.IsGranted,
                    GrantedAt: c.CreatedAt,
                    RevokedAt: c.IsGranted ? null : c.UpdatedAt,
                    IpAddress: null,
                    UserAgent: null
                )),
                SavedCards: client.SavedCards.Select(sc => new SavedCardDto(
                    Id: sc.Id,
                    Nickname: sc.Nickname ?? "Cartão",
                    Brand: sc.Brand.ToString(),
                    LastFourDigits: sc.LastFourDigits,
                    ExpiryDate: sc.ExpiryDate,
                    IsDefault: sc.IsDefault
                )),
                Timestamp: DateTime.UtcNow
            );

            _logger.LogInformation("Perfil do cliente recuperado com sucesso. ClientId: {ClientId}", client.Id);
            return Result<GetClientProfileOutput, Notification>.Ok(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno durante busca do perfil do cliente");
            notification.Add(new Error("profile.internalError", $"Erro interno durante busca do perfil: {ex.Message}"));
            return Result<GetClientProfileOutput, Notification>.Fail(notification);
        }
    }
}