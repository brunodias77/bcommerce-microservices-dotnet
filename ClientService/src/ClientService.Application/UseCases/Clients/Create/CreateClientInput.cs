namespace ClientService.Application.UseCases.Clients.Create;

public record CreateClientInput(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string? Role = "USER" // USER ou ADMIN
);