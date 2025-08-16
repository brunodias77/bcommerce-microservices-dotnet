using ClientService.Application.Common;
using ClientService.Application.Services;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.Repositories;
using ClientService.Domain.Validations;

namespace ClientService.Application.UseCases.Clients.Create;

public class CreateClientUseCase : ICreateClientUseCase
{
    public CreateClientUseCase(IKeycloakService keycloakService, IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _keycloakService = keycloakService;
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
    }

    private readonly IKeycloakService _keycloakService;
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;
    
    public async Task<Result<CreateClientOutput, Notification>> Execute(CreateClientInput input)
    {
        var notification = new Notification();
        ValidateInput(input, notification);
        if (notification.HasErrors)
            return Result<CreateClientOutput, Notification>.Fail(notification);
        
        var existingClient = await _clientRepository.GetByEmailAsync(input.Email);
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
            // Criar a entidade Client
            var client = Client.Create(
                keycloakResult.Value?.UserId != null ? Guid.Parse(keycloakResult.Value.UserId) : null,
                input.FirstName,
                input.LastName,
                input.Email,
                string.Empty, // PasswordHash vazio pois a senha está no Keycloak
                input.Role ?? "USER"
            );
            
            // Iniciar transação
            await _unitOfWork.BeginTransactionAsync();
            
            // Salvar o cliente no banco de dados
            await _clientRepository.AddAsync(client);
            await _unitOfWork.SaveChangesAsync();
            
            // Confirmar a transação
            await _unitOfWork.CommitTransactionAsync();
            
            // Retornar sucesso
            var output = new CreateClientOutput(
                Message: "Cliente criado com sucesso",
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
            // Rollback da transação em caso de erro
            await _unitOfWork.RollbackTransactionAsync();
            
            // Tentar excluir o usuário do Keycloak se a criação do cliente falhar
            if (keycloakResult.Value?.UserId != null)
            {
                try
                {
                    // Aqui você pode implementar a exclusão do usuário do Keycloak
                    // await _keycloakService.DeleteUserAsync(keycloakResult.Value.UserId);
                }
                catch
                {
                    // Log do erro, mas não falha a operação principal
                }
            }
            
            notification.Add(new Error("database", $"Erro ao salvar cliente no banco de dados: {ex.Message}"));
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