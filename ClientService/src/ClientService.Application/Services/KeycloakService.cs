using ClientService.Domain.Validations;
using ClientService.Application.Common;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace ClientService.Application.Services;

public class KeycloakService : IKeycloakService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public KeycloakService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    /// <summary>
    /// Obtém token de administrador do Keycloak para operações administrativas
    /// </summary>
    /// <returns>Token de acesso ou null em caso de falha</returns>
    public async Task<string?> GetAdminTokenAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("keycloak");
            
            // Preparar requisição de token usando credenciais de admin
            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", _configuration["Keycloak:AdminClientId"] ?? "admin-cli"),
                new KeyValuePair<string, string>("username", _configuration["Keycloak:AdminUsername"] ?? "admin"),
                new KeyValuePair<string, string>("password", _configuration["Keycloak:AdminPassword"] ?? "admin")
            });

            var response = await httpClient.PostAsync("realms/master/protocol/openid-connect/token", tokenRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(content);
                return document.RootElement.GetProperty("access_token").GetString();
            }
            
            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Cria usuário no Keycloak e atribui role especificada
    /// </summary>
    /// <param name="request">Dados do usuário a ser criado</param>
    /// <returns>Resultado da operação com ID do usuário e status da atribuição de role</returns>
    public async Task<Result<KeycloakCreateUserResult, Notification>> CreateUserWithRoleAsync(CreateKeycloakUserRequest request)
    {
        var notification = new Notification();
        
        try
        {
            // 1. Obter token de admin
            var adminToken = await GetAdminTokenAsync();
            if (string.IsNullOrEmpty(adminToken))
            {
                notification.Add(new Error("", "Falha ao obter token de administrador do Keycloak"));
                return Result<KeycloakCreateUserResult, Notification>.Fail(notification);
            }

            // 2. Configurar cliente HTTP com autenticação
            var httpClient = _httpClientFactory.CreateClient("keycloak");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            // 3. Preparar dados do usuário para Keycloak
            var keycloakUser = new
            {
                username = request.Username,
                email = request.Email,
                firstName = request.FirstName,
                lastName = request.LastName,
                enabled = true,
                emailVerified = true,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = request.Password,
                        temporary = false
                    }
                },
                realmRoles = new[] { request.Role ?? "USER" }
            };

            // 4. Serializar e enviar requisição de criação
            var json = JsonSerializer.Serialize(keycloakUser);
            var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

            var realm = _configuration["Keycloak:Realm"];
            var response = await httpClient.PostAsync($"admin/realms/{realm}/users", content);

            if (response.IsSuccessStatusCode)
            {
                // 5. Extrair ID do usuário criado do header Location
                var location = response.Headers.Location?.ToString();
                var userId = location?.Split('/').LastOrDefault();

                if (string.IsNullOrEmpty(userId))
                {
                    notification.Add(new Error("", "Usuário criado mas não foi possível obter o ID"));
                    return Result<KeycloakCreateUserResult, Notification>.Fail(notification);
                }

                // 6. Atribuir role ao usuário (se especificada)
                bool roleAssigned = true;
                if (!string.IsNullOrEmpty(request.Role))
                {
                    roleAssigned = await AssignRoleToUserAsync(userId, request.Role);
                }

                var result = new KeycloakCreateUserResult(userId, roleAssigned);
                return Result<KeycloakCreateUserResult, Notification>.Ok(result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                notification.Add(new Error("",$"Falha ao criar usuário no Keycloak: {errorContent}"));
                return Result<KeycloakCreateUserResult, Notification>.Fail(notification);
            }
        }
        catch (Exception ex)
        {
            notification.Add(new Error("", $"Erro interno ao criar usuário: {ex.Message}"));
            return Result<KeycloakCreateUserResult, Notification>.Fail(notification);
        }
    }

    /// <summary>
    /// Atribui uma role específica a um usuário existente no Keycloak
    /// </summary>
    /// <param name="userId">ID do usuário no Keycloak</param>
    /// <param name="roleName">Nome da role a ser atribuída</param>
    /// <returns>True se a operação foi bem-sucedida</returns>
    public async Task<bool> AssignRoleToUserAsync(string userId, string roleName)
    {
        try
        {
            // 1. Obter token de admin
            var adminToken = await GetAdminTokenAsync();
            if (string.IsNullOrEmpty(adminToken))
                return false;

            var httpClient = _httpClientFactory.CreateClient("keycloak");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var realm = _configuration["Keycloak:Realm"];

            // 2. Obter informações da role pelo nome
            var rolesResponse = await httpClient.GetAsync($"admin/realms/{realm}/roles");
            if (!rolesResponse.IsSuccessStatusCode) 
                return false;

            var rolesContent = await rolesResponse.Content.ReadAsStringAsync();
            using var rolesDocument = JsonDocument.Parse(rolesContent);
            var roles = rolesDocument.RootElement.EnumerateArray();
            var role = roles.FirstOrDefault(r => r.GetProperty("name").GetString() == roleName);

            if (!role.ValueKind.Equals(JsonValueKind.Undefined))
            {
                // 3. Preparar dados para atribuição de role
                var roleAssignment = new[]
                {
                    new
                    {
                        id = role.GetProperty("id").GetString(),
                        name = role.GetProperty("name").GetString()
                    }
                };
                
                var json = JsonSerializer.Serialize(roleAssignment);
                var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);

                // 4. Atribuir role ao usuário
                var assignResponse = await httpClient.PostAsync(
                    $"admin/realms/{realm}/users/{userId}/role-mappings/realm", 
                    content
                );
                
                return assignResponse.IsSuccessStatusCode;
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Deleta um usuário do Keycloak
    /// </summary>
    /// <param name="userId">ID do usuário a ser deletado</param>
    /// <returns>Resultado da operação de deleção</returns>
    public async Task<Result<bool, Notification>> DeleteUserAsync(string userId)
    {
        var notification = new Notification();
        
        try
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(userId))
            {
                notification.Add(new Error("", "ID do usuário é obrigatório"));
                return Result<bool, Notification>.Fail(notification);
            }

            // 1. Obter token de admin
            var adminToken = await GetAdminTokenAsync();
            if (string.IsNullOrEmpty(adminToken))
            {
                notification.Add(new Error("", "Falha ao obter token de administrador do Keycloak"));
                return Result<bool, Notification>.Fail(notification);
            }

            // 2. Configurar cliente HTTP com autenticação
            var httpClient = _httpClientFactory.CreateClient("keycloak");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

            var realm = _configuration["Keycloak:Realm"];
            
            // 3. Verificar se o usuário existe antes de tentar deletar
            var getUserResponse = await httpClient.GetAsync($"admin/realms/{realm}/users/{userId}");
            if (getUserResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                notification.Add(new Error("", "Usuário não encontrado no Keycloak"));
                return Result<bool, Notification>.Fail(notification);
            }
            
            if (!getUserResponse.IsSuccessStatusCode)
            {
                var errorContent = await getUserResponse.Content.ReadAsStringAsync();
                notification.Add(new Error("", $"Erro ao verificar usuário: {errorContent}"));
                return Result<bool, Notification>.Fail(notification);
            }

            // 4. Deletar o usuário
            var deleteResponse = await httpClient.DeleteAsync($"admin/realms/{realm}/users/{userId}");
            
            if (deleteResponse.IsSuccessStatusCode)
            {
                return Result<bool, Notification>.Ok(true);
            }
            else
            {
                var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                notification.Add(new Error("", $"Falha ao deletar usuário no Keycloak: {errorContent}"));
                return Result<bool, Notification>.Fail(notification);
            }
        }
        catch (Exception ex)
        {
            notification.Add(new Error("", $"Erro interno ao deletar usuário: {ex.Message}"));
            return Result<bool, Notification>.Fail(notification);
        }
    }

    /// <summary>
    /// Autentica usuário no Keycloak usando suas credenciais
    /// </summary>
    /// <param name="request">Dados de login do usuário</param>
    /// <returns>Resultado da operação com token de acesso</returns>
    public async Task<Result<KeycloakLoginResult, Notification>> LoginAsync(KeycloakLoginRequest request)
    {
        var notification = new Notification();
        
        try
        {
            // Validar entrada
            if (string.IsNullOrWhiteSpace(request.Username))
            {
                notification.Add(new Error("", "Nome de usuário é obrigatório"));
                return Result<KeycloakLoginResult, Notification>.Fail(notification);
            }
            
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                notification.Add(new Error("", "Senha é obrigatória"));
                return Result<KeycloakLoginResult, Notification>.Fail(notification);
            }

            var httpClient = _httpClientFactory.CreateClient("keycloak");
            var realm = _configuration["Keycloak:Realm"];
            var clientId = _configuration["Keycloak:ClientId"] ?? "b-commerce-client";
            var clientSecret = _configuration["Keycloak:ClientSecret"];
            
            // Preparar requisição de token usando credenciais do usuário
            var tokenRequestParams = new List<KeyValuePair<string, string>>
            {
                new("grant_type", "password"),
                new("client_id", clientId),
                new("username", request.Username),
                new("password", request.Password)
            };
            
            // Adicionar client_secret se configurado
            if (!string.IsNullOrEmpty(clientSecret))
            {
                tokenRequestParams.Add(new KeyValuePair<string, string>("client_secret", clientSecret));
            }
            
            var tokenRequest = new FormUrlEncodedContent(tokenRequestParams);
            
            var response = await httpClient.PostAsync($"realms/{realm}/protocol/openid-connect/token", tokenRequest);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                
                var accessToken = root.GetProperty("access_token").GetString();
                var refreshToken = root.TryGetProperty("refresh_token", out var refreshProp) ? refreshProp.GetString() : "";
                var expiresIn = root.TryGetProperty("expires_in", out var expiresProp) ? expiresProp.GetInt32() : 3600;
                var tokenType = root.TryGetProperty("token_type", out var typeProp) ? typeProp.GetString() : "Bearer";
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    notification.Add(new Error("", "Token de acesso não foi retornado pelo Keycloak"));
                    return Result<KeycloakLoginResult, Notification>.Fail(notification);
                }
                
                var result = new KeycloakLoginResult(
                    accessToken,
                    refreshToken ?? "",
                    expiresIn,
                    tokenType ?? "Bearer"
                );
                
                return Result<KeycloakLoginResult, Notification>.Ok(result);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                
                // Tratar diferentes tipos de erro
                var errorMessage = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "Credenciais inválidas",
                    System.Net.HttpStatusCode.BadRequest => "Requisição inválida",
                    _ => $"Falha na autenticação: {errorContent}"
                };
                
                notification.Add(new Error("", errorMessage));
                return Result<KeycloakLoginResult, Notification>.Fail(notification);
            }
        }
        catch (Exception ex)
        {
            notification.Add(new Error("", $"Erro interno durante login: {ex.Message}"));
            return Result<KeycloakLoginResult, Notification>.Fail(notification);
        }
    }
}