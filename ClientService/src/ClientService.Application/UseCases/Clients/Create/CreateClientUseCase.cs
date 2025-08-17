using ClientService.Application.Common;
using ClientService.Application.Services;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.Repositories;
using ClientService.Domain.Validations;
using ClientService.Domain.ValueObjects;
using ClientService.Domain.Enums;
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
    
        // 2. Iniciar transação para operações no banco
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // 3. Criar cliente no banco de dados
            var client = Client.Create(
                Guid.Parse(keycloakResult.Value?.UserId ?? string.Empty),
                input.FirstName,
                input.LastName,
                input.Email,
                input.Password, // Será hasheado no domínio se necessário
                input.Role ?? "USER"
            );

            await _clientRepository.AddAsync(client);
            
            // 4. Salvar mudanças no banco
            await _unitOfWork.SaveChangesAsync();
            
            // 5. Publicar eventos do domínio
            if (client.HasEvents)
            {
                await _domainEventPublisher.PublishAsync(client.Events);
                client.ClearEvents();
            }
            
            // 6. Confirmar transação
            await _unitOfWork.CommitTransactionAsync();
            
            var output = new CreateClientOutput(
                Message: "Cliente criado com sucesso.",
                UserId: client.Id.ToString(),
                Username: input.Username,
                Email: input.Email,
                Role: input.Role ?? "USER",
                Timestamp: DateTime.UtcNow
            );
            
            return Result<CreateClientOutput, Notification>.Ok(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao criar cliente para usuário Keycloak {UserId}", keycloakResult.Value?.UserId);
            
            // Rollback da transação
            await _unitOfWork.RollbackTransactionAsync();
            
            // Se falhar ao salvar cliente, tentar excluir usuário do Keycloak para manter consistência
            if (keycloakResult.Value?.UserId != null)
            {
                try
                {
                    // await _keycloakService.DeleteUserAsync(keycloakResult.Value.UserId);
                    _logger.LogInformation("Usuário Keycloak {UserId} removido devido a falha na criação do cliente", keycloakResult.Value.UserId);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogError(rollbackEx, "Falha ao remover usuário Keycloak {UserId} durante rollback", keycloakResult.Value.UserId);
                }
            }
            
            notification.Add(new Error("client", $"Erro ao criar cliente: {ex.Message}"));
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