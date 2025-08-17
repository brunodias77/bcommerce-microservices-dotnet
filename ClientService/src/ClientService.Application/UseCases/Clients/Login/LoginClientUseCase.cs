using ClientService.Application.Common;
using ClientService.Application.Services;
using ClientService.Domain.Common;
using ClientService.Domain.Repositories;
using ClientService.Domain.Validations;
using ClientService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ClientService.Application.UseCases.Clients.Login;

public class LoginClientUseCase : ILoginClientUseCase
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakService _keycloakService;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly ILogger<LoginClientUseCase> _logger;

    public LoginClientUseCase(
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork,
        IKeycloakService keycloakService,
        IDomainEventPublisher domainEventPublisher,
        ILogger<LoginClientUseCase> logger)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
        _keycloakService = keycloakService;
        _domainEventPublisher = domainEventPublisher;
        _logger = logger;
    }

    public async Task<Result<LoginClientOutput, Notification>> Execute(LoginClientInput input)
    {
        var notification = new Notification();
        
        try
        {
            _logger.LogInformation("Iniciando processo de login para email: {Email}", input.Email);
            
            // 1. Validar entrada
            ValidateInput(input, notification);
            if (notification.HasErrors)
            {
                _logger.LogWarning("Falha na validação de entrada para login: {Email}", input.Email);
                return Result<LoginClientOutput, Notification>.Fail(notification);
            }

            // 2. Verificar se o cliente existe no banco de dados
            var emailValueObject = Email.Create(input.Email);
            var existingClient = await _clientRepository.GetByEmailAsync(emailValueObject);
            
            if (existingClient == null)
            {
                _logger.LogWarning("Tentativa de login com email não cadastrado: {Email}", input.Email);
                notification.Add(new Error("client.notFound", "Email não encontrado"));
                return Result<LoginClientOutput, Notification>.Fail(notification);
            }

            // 3. Verificar se o cliente está ativo
            if (existingClient.Status != Domain.Enums.ClientStatus.Ativo)
            {
                _logger.LogWarning("Tentativa de login com cliente inativo: {Email}, Status: {Status}", input.Email, existingClient.Status);
                notification.Add(new Error("client.inactive", "Cliente inativo ou banido"));
                return Result<LoginClientOutput, Notification>.Fail(notification);
            }

            // 4. Autenticar no Keycloak usando email como username
            var keycloakLoginRequest = new KeycloakLoginRequest(input.Email, input.Password);
            var keycloakResult = await _keycloakService.LoginAsync(keycloakLoginRequest);
            
            if (!keycloakResult.IsSuccess)
            {
                _logger.LogWarning("Falha na autenticação Keycloak para email: {Email}", input.Email);
                
                // Propagar erros do Keycloak
                if (keycloakResult.Error?.Errors != null)
                {
                    foreach (var error in keycloakResult.Error.Errors)
                    {
                        notification.Add(error);
                    }
                }
                else
                {
                    notification.Add(new Error("authentication.failed", "Falha na autenticação"));
                }
                
                return Result<LoginClientOutput, Notification>.Fail(notification);
            }

            // 5. Login bem-sucedido - criar output
            var loginResult = keycloakResult.Value!;
            var output = new LoginClientOutput(
                AccessToken: loginResult.AccessToken,
                TokenType: loginResult.TokenType,
                ExpiresIn: loginResult.ExpiresIn,
                RefreshToken: loginResult.RefreshToken,
                Timestamp: DateTime.UtcNow
            );

            _logger.LogInformation("Login realizado com sucesso para email: {Email}, ClientId: {ClientId}", 
                input.Email, existingClient.Id);

            return Result<LoginClientOutput, Notification>.Ok(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno durante processo de login para email: {Email}", input.Email);
            notification.Add(new Error("login.internalError", $"Erro interno durante login: {ex.Message}"));
            return Result<LoginClientOutput, Notification>.Fail(notification);
        }
    }

    private static void ValidateInput(LoginClientInput input, Notification notification)
    {
        if (string.IsNullOrWhiteSpace(input.Email))
        {
            notification.Add(new Error("input.Email", "Email é obrigatório"));
        }
        else if (!IsValidEmail(input.Email))
        {
            notification.Add(new Error("input.Email", "Email deve ter um formato válido"));
        }

        if (string.IsNullOrWhiteSpace(input.Password))
        {
            notification.Add(new Error("input.Password", "Senha é obrigatória"));
        }
        else if (input.Password.Length < 6)
        {
            notification.Add(new Error("input.Password", "Senha deve ter pelo menos 6 caracteres"));
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}