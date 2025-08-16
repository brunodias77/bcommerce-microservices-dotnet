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
}