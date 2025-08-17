namespace ClientService.Application.UseCases.Clients.Login;

public record LoginClientOutput(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken,
    DateTime Timestamp
);