namespace ClientService.Application.UseCases.Clients.GetProfile;

public record GetClientProfileInput(
    string KeycloakUserId
);