using ClientService.Application.Common;
using ClientService.Domain.Validations;

namespace ClientService.Application.Services;

public interface IKeycloakService
{
    /// <summary>
    /// Obtém token de administrador do Keycloak
    /// </summary>
    /// <returns>Token de acesso ou null em caso de falha</returns>
    Task<string?> GetAdminTokenAsync();
    
    /// <summary>
    /// Cria usuário no Keycloak com role atribuída
    /// </summary>
    /// <param name="request">Dados do usuário a ser criado</param>
    /// <returns>Resultado da operação com ID do usuário criado</returns>
    Task<Result<KeycloakCreateUserResult, Notification>> CreateUserWithRoleAsync(CreateKeycloakUserRequest request);

    /// <summary>
    /// Atribui role a um usuário existente
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <param name="roleName">Nome da role</param>
    /// <returns>True se a operação foi bem-sucedida</returns>
    Task<bool> AssignRoleToUserAsync(string userId, string roleName);

}

public record CreateKeycloakUserRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string? Role = "USER"
);


public record KeycloakCreateUserResult(
    string UserId,
    bool RoleAssigned
);