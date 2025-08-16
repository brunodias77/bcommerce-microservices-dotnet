namespace ClientService.Application.UseCases.Clients.Create;

public record CreateClientOutput(
    string Message,
    string? UserId,
    string Username,
    string Email,
    string Role,
    DateTime Timestamp
);