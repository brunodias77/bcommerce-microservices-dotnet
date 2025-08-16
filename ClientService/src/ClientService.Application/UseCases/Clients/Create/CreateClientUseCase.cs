using ClientService.Application.Common;
using ClientService.Application.Services;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.Events.Clients;
using ClientService.Domain.Repositories;
using ClientService.Domain.Validations;
using ClientService.Domain.ValueObjects;
using ClientService.Domain.Enums; // Adicionar este using
using Microsoft.Extensions.Logging;

namespace ClientService.Application.UseCases.Clients.Create;

public class CreateClientUseCase : ICreateClientUseCase
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakService _keycloakService;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly ILogger<CreateClientUseCase> _logger;

    public CreateClientUseCase(
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork,
        IKeycloakService keycloakService,
        IDomainEventPublisher domainEventPublisher,
        ILogger<CreateClientUseCase> logger)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
        _keycloakService = keycloakService;
        _domainEventPublisher = domainEventPublisher;
        _logger = logger;
    }

    public async Task<Result<CreateClientOutput, Notification>> Execute(CreateClientInput input)
    {
        var notification = new Notification();
        ValidateInput(input, notification);
        if (notification.HasErrors)
            return Result<CreateClientOutput, Notification>.Fail(notification);
        
        // Verificar se já existe cliente com este email
        var existingClient = await _clientRepository.GetByEmailAsync(Email.Create(input.Email));
        if (existingClient != null)
        {
            notification.Add(new Error("existingClient", "Já existe um cliente cadastrado com este email"));
            return Result<CreateClientOutput, Notification>.Fail(notification);
        }
        
        var keycloakRequest = new CreateKeycloakUserRequest(
            input.Username,
            input.Email,
            input.FirstName,
            input.LastName,
            input.Password,
            input.Role
        );
        
        // 1. Criar usuário no Keycloak
        var keycloakResult = await _keycloakService.CreateUserWithRoleAsync(keycloakRequest);
        if (!keycloakResult.IsSuccess)
        {
            if (keycloakResult.Error?.Errors != null)
            {
                foreach (var error in keycloakResult.Error.Errors)
                {
                    notification.Add(error);
                }
            }
            return Result<CreateClientOutput, Notification>.Fail(notification);
        }
    
        try
        {
            // 2. Disparar evento para criar cliente no banco
            var createClientEvent = new CreateClientKeycloak(
                keycloakResult.Value?.UserId ?? string.Empty,
                input.Username,
                input.Email,
                input.FirstName,
                input.LastName,
                Enum.Parse<UserRole>(input.Role ?? "USER"),
                null, // Phone - não disponível no CreateClientInput
                null, // DateOfBirth - não disponível no CreateClientInput  
                null, // Cpf - não disponível no CreateClientInput
                false // NewsletterOptIn - não disponível no CreateClientInput
            );
    
            await _domainEventPublisher.PublishAsync(createClientEvent);
            
            // Retornar sucesso imediatamente após disparar o evento
            var output = new CreateClientOutput(
                Message: "Usuário criado no Keycloak com sucesso. Cliente será criado em breve.",
                UserId: keycloakResult.Value?.UserId?.ToString(),
                Username: input.Username,
                Email: input.Email,
                Role: input.Role ?? "USER",
                Timestamp: DateTime.UtcNow
            );
            
            return Result<CreateClientOutput, Notification>.Ok(output);
        }
        catch (Exception ex)
        {
            // Se falhar ao disparar evento, tentar excluir usuário do Keycloak
            if (keycloakResult.Value?.UserId != null)
            {
                try
                {
                    // await _keycloakService.DeleteUserAsync(keycloakResult.Value.UserId);
                }
                catch
                {
                    // Log do erro, mas não falha a operação principal
                }
            }
            
            notification.Add(new Error("event", $"Erro ao processar criação do cliente: {ex.Message}"));
            return Result<CreateClientOutput, Notification>.Fail(notification);
        }
    }
    
    private static void ValidateInput(CreateClientInput input, Notification notification)
    {
        if (string.IsNullOrWhiteSpace(input.Username))
            notification.Add(new Error("input.Username", "Username é obrigatório"));
        
        if (string.IsNullOrWhiteSpace(input.Email))
            notification.Add(new Error( "input.Email","Email é obrigatório"));
        
        if (string.IsNullOrWhiteSpace(input.FirstName))
            notification.Add(new Error("input.FirstName","Nome é obrigatório"));
        
        if (string.IsNullOrWhiteSpace(input.LastName))
            notification.Add(new Error("input.LastName","Sobrenome é obrigatório"));
        
        if (string.IsNullOrWhiteSpace(input.Password))
            notification.Add(new Error("input.Password","Senha é obrigatória"));
    }
}