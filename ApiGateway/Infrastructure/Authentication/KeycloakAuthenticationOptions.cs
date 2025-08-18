using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace ApiGateway.Infrastructure.Authentication;

/// <summary>
/// Opções de configuração para autenticação com Keycloak
/// </summary>
public class KeycloakAuthenticationOptions
{
    /// <summary>
    /// URL base do servidor Keycloak
    /// </summary>
    public string Authority { get; set; } = string.Empty;
    
    /// <summary>
    /// Nome do realm no Keycloak
    /// </summary>
    public string Realm { get; set; } = string.Empty;
    
    /// <summary>
    /// Client ID configurado no Keycloak
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Client Secret (se necessário)
    /// </summary>
    public string? ClientSecret { get; set; }
    
    /// <summary>
    /// Audience principal para validação de token
    /// </summary>
    public string Audience { get; set; } = string.Empty;
    
    /// <summary>
    /// Se deve exigir HTTPS para metadata
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
    
    /// <summary>
    /// Lista de issuers válidos
    /// </summary>
    public string[] ValidIssuers { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Lista de audiences válidas
    /// </summary>
    public string[] ValidAudiences { get; set; } = Array.Empty<string>();
    
    /// <summary>
    /// Endpoints públicos que não requerem autenticação
    /// </summary>
    public List<string> PublicEndpoints { get; set; } = new();
    
    /// <summary>
    /// Tempo de tolerância para validação de token (em segundos)
    /// </summary>
    public int ClockSkewSeconds { get; set; } = 300;
    
    /// <summary>
    /// Se deve validar o tempo de vida do token
    /// </summary>
    public bool ValidateLifetime { get; set; } = true;
    
    /// <summary>
    /// Se deve validar o issuer
    /// </summary>
    public bool ValidateIssuer { get; set; } = true;
    
    /// <summary>
    /// Se deve validar a audience
    /// </summary>
    public bool ValidateAudience { get; set; } = true;
    
    /// <summary>
    /// Se deve validar a chave de assinatura
    /// </summary>
    public bool ValidateIssuerSigningKey { get; set; } = true;
    
    /// <summary>
    /// Configurações para refresh de token
    /// </summary>
    public TokenRefreshOptions TokenRefresh { get; set; } = new();
    
    /// <summary>
    /// URL do endpoint de refresh token
    /// </summary>
    public string RefreshTokenEndpoint => $"{Authority}/realms/{Realm}/protocol/openid-connect/token";
    
    /// <summary>
    /// URL do endpoint de logout
    /// </summary>
    public string LogoutEndpoint => $"{Authority}/realms/{Realm}/protocol/openid-connect/logout";
    
    /// <summary>
    /// URL do endpoint de informações do usuário
    /// </summary>
    public string UserInfoEndpoint => $"{Authority}/realms/{Realm}/protocol/openid-connect/userinfo";
}

/// <summary>
/// Opções para configuração de refresh de token
/// </summary>
public class TokenRefreshOptions
{
    /// <summary>
    /// Se deve tentar fazer refresh automático do token
    /// </summary>
    public bool AutoRefresh { get; set; } = true;
    
    /// <summary>
    /// Tempo em minutos antes da expiração para tentar refresh
    /// </summary>
    public int RefreshBeforeExpirationMinutes { get; set; } = 5;
    
    /// <summary>
    /// Número máximo de tentativas de refresh
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;
    
    /// <summary>
    /// Intervalo entre tentativas de refresh em segundos
    /// </summary>
    public int RetryIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// URL do endpoint de refresh token
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Client ID para refresh
    /// </summary>
    public string ClientId { get; set; } = string.Empty;
    
    /// <summary>
    /// Client Secret para refresh
    /// </summary>
    public string? ClientSecret { get; set; }
}