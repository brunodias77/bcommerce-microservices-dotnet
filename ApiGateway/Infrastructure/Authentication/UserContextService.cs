using System.Security.Claims;

namespace ApiGateway.Infrastructure.Authentication;

/// <summary>
/// Implementação do serviço de contexto do usuário
/// </summary>
public class UserContextService : IUserContextService
{
    private const string UserContextKey = "UserContext";
    private readonly ILogger<UserContextService> _logger;
    
    public UserContextService(ILogger<UserContextService> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Extrai o contexto do usuário a partir de um ClaimsPrincipal
    /// </summary>
    /// <param name="principal">ClaimsPrincipal do usuário</param>
    /// <returns>UserContext populado</returns>
    public UserContext ExtractUserContext(ClaimsPrincipal principal)
    {
        try
        {
            var context = UserContext.FromClaimsPrincipal(principal);
            
            _logger.LogDebug("Contexto do usuário extraído: {UserId} - {Username}", 
                context.UserId, context.Username);
                
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair contexto do usuário");
            return new UserContext { IsAuthenticated = false };
        }
    }
    
    /// <summary>
    /// Obtém o contexto do usuário atual da requisição HTTP
    /// </summary>
    /// <param name="httpContext">HttpContext da requisição</param>
    /// <returns>UserContext se disponível, null caso contrário</returns>
    public UserContext? GetCurrentUserContext(HttpContext httpContext)
    {
        return httpContext.Items[UserContextKey] as UserContext;
    }
    
    /// <summary>
    /// Armazena o contexto do usuário no HttpContext
    /// </summary>
    /// <param name="httpContext">HttpContext da requisição</param>
    /// <param name="userContext">Contexto do usuário</param>
    public void SetUserContext(HttpContext httpContext, UserContext userContext)
    {
        httpContext.Items[UserContextKey] = userContext;
        
        _logger.LogDebug("Contexto do usuário armazenado no HttpContext: {UserId}", 
            userContext.UserId);
    }
}