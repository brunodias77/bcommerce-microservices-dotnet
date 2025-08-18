using System.Security.Claims;

namespace ApiGateway.Infrastructure.Authentication;

/// <summary>
/// Contexto do usuário extraído do token JWT
/// </summary>
public class UserContext
{
    /// <summary>
    /// ID único do usuário
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome de usuário
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Email do usuário
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome completo do usuário
    /// </summary>
    public string FullName { get; set; } = string.Empty;
    
    /// <summary>
    /// Roles/funções do usuário
    /// </summary>
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Permissões específicas do usuário
    /// </summary>
    public List<string> Permissions { get; set; } = new();
    
    /// <summary>
    /// Claims adicionais do token
    /// </summary>
    public Dictionary<string, string> AdditionalClaims { get; set; } = new();
    
    /// <summary>
    /// Timestamp de quando o token expira
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// Se o usuário está autenticado
    /// </summary>
    public bool IsAuthenticated { get; set; }
    
    /// <summary>
    /// Token de acesso original
    /// </summary>
    public string? AccessToken { get; set; }
    
    /// <summary>
    /// Token de refresh (se disponível)
    /// </summary>
    public string? RefreshToken { get; set; }
    
    /// <summary>
    /// Verifica se o usuário possui uma role específica
    /// </summary>
    /// <param name="role">Nome da role</param>
    /// <returns>True se o usuário possui a role</returns>
    public bool HasRole(string role)
    {
        return Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Verifica se o usuário possui uma permissão específica
    /// </summary>
    /// <param name="permission">Nome da permissão</param>
    /// <returns>True se o usuário possui a permissão</returns>
    public bool HasPermission(string permission)
    {
        return Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Cria um UserContext a partir de um ClaimsPrincipal
    /// </summary>
    /// <param name="principal">ClaimsPrincipal do usuário</param>
    /// <returns>UserContext populado</returns>
    public static UserContext FromClaimsPrincipal(ClaimsPrincipal principal)
    {
        var context = new UserContext
        {
            IsAuthenticated = principal.Identity?.IsAuthenticated ?? false
        };
        
        if (!context.IsAuthenticated)
            return context;
            
        context.UserId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        context.Username = principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        context.Email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        context.FullName = principal.FindFirst("name")?.Value ?? string.Empty;
        
        // Extrair roles
        context.Roles = principal.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToList();
            
        // Extrair permissões (se existirem)
        context.Permissions = principal.FindAll("permissions")
            .Select(c => c.Value)
            .ToList();
            
        // Extrair claims adicionais
        foreach (var claim in principal.Claims)
        {
            if (!context.AdditionalClaims.ContainsKey(claim.Type))
            {
                context.AdditionalClaims[claim.Type] = claim.Value;
            }
        }
        
        // Extrair tempo de expiração
        var expClaim = principal.FindFirst("exp")?.Value;
        if (long.TryParse(expClaim, out var exp))
        {
            context.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
        }
        
        return context;
    }
}