using System.Security.Claims;

namespace ApiGateway.Infrastructure.Authentication;

/// <summary>
/// Serviço para extração e gerenciamento do contexto do usuário
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Extrai o contexto do usuário a partir de um ClaimsPrincipal
    /// </summary>
    /// <param name="principal">ClaimsPrincipal do usuário</param>
    /// <returns>UserContext populado</returns>
    UserContext ExtractUserContext(ClaimsPrincipal principal);
    
    /// <summary>
    /// Obtém o contexto do usuário atual da requisição HTTP
    /// </summary>
    /// <param name="httpContext">HttpContext da requisição</param>
    /// <returns>UserContext se disponível, null caso contrário</returns>
    UserContext? GetCurrentUserContext(HttpContext httpContext);
    
    /// <summary>
    /// Armazena o contexto do usuário no HttpContext
    /// </summary>
    /// <param name="httpContext">HttpContext da requisição</param>
    /// <param name="userContext">Contexto do usuário</param>
    void SetUserContext(HttpContext httpContext, UserContext userContext);
}