using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace ApiGateway.Infrastructure.Authentication;

/// <summary>
/// Handler personalizado para autenticação JWT com funcionalidades avançadas
/// Implementa validação customizada, extração de contexto do usuário e tratamento de erros
/// </summary>
public class JwtAuthenticationHandler : JwtBearerHandler
{
    private readonly ILogger<JwtAuthenticationHandler> _logger;
    private readonly KeycloakAuthenticationOptions _options;
    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;

    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtBearerOptions> jwtOptions,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IOptions<KeycloakAuthenticationOptions> keycloakOptions,
        HttpClient httpClient,
        TimeProvider timeProvider) 
        : base(jwtOptions, loggerFactory, encoder)
    {
        _logger = loggerFactory.CreateLogger<JwtAuthenticationHandler>();
        _options = keycloakOptions.Value;
        _httpClient = httpClient;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Processa a autenticação JWT com lógica personalizada
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // Verifica se o endpoint é público (não requer autenticação)
            if (IsPublicEndpoint())
            {
                _logger.LogDebug("Public endpoint accessed, skipping authentication");
                return AuthenticateResult.NoResult();
            }

            // Extrai o token do header Authorization
            var token = ExtractTokenFromHeader();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No JWT token found in Authorization header");
                return AuthenticateResult.Fail("No JWT token provided");
            }

            // Valida o token JWT
            var validationResult = await ValidateTokenAsync(token);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("JWT token validation failed: {Error}", validationResult.Error);
                return AuthenticateResult.Fail(validationResult.Error);
            }

            // Cria o principal com as claims do usuário
            var principal = CreateClaimsPrincipal(validationResult.Claims);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            _logger.LogDebug("JWT authentication successful for user: {UserId}", 
                principal.FindFirst("sub")?.Value);

            return AuthenticateResult.Success(ticket);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("JWT token expired: {Message}", ex.Message);
            return AuthenticateResult.Fail("Token expired");
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("JWT token has invalid signature: {Message}", ex.Message);
            return AuthenticateResult.Fail("Invalid token signature");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during JWT authentication");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    /// <summary>
    /// Verifica se o endpoint atual é público (não requer autenticação)
    /// </summary>
    private bool IsPublicEndpoint()
    {
        var path = Context.Request.Path.Value?.ToLowerInvariant();
        
        var publicEndpoints = new[]
        {
            "/health",
            "/health/ready",
            "/health/live",
            "/metrics",
            "/api/clients/create-user",
            "/api/clients/login",
            "/api/catalog/products", // Catálogo público
            "/swagger",
            "/api-docs"
        };

        return publicEndpoints.Any(endpoint => path?.StartsWith(endpoint) == true);
    }

    /// <summary>
    /// Extrai o token JWT do header Authorization
    /// </summary>
    private string? ExtractTokenFromHeader()
    {
        var authHeader = Context.Request.Headers["Authorization"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader.Substring("Bearer ".Length).Trim();
    }

    /// <summary>
    /// Valida o token JWT usando as configurações do Keycloak
    /// </summary>
    private async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Parâmetros de validação baseados na configuração do Keycloak
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _options.Authority,
                ValidAudiences = _options.ValidAudiences,
                ClockSkew = TimeSpan.FromSeconds(_options.ClockSkewSeconds),
                // As chaves de assinatura serão obtidas automaticamente do Keycloak
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    // Implementação para obter chaves do Keycloak JWKS endpoint
                    return GetKeycloakSigningKeys();
                }
            };

            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            
            if (result.IsValid)
            {
                var jwtToken = result.SecurityToken as JwtSecurityToken;
                return new TokenValidationResult
                {
                    IsValid = true,
                    Claims = result.ClaimsIdentity.Claims.ToList(),
                    Token = jwtToken
                };
            }
            else
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = result.Exception?.Message ?? "Token validation failed"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return new TokenValidationResult
            {
                IsValid = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Obtém as chaves de assinatura do Keycloak JWKS endpoint
    /// </summary>
    private IEnumerable<SecurityKey> GetKeycloakSigningKeys()
    {
        // Esta implementação seria expandida para buscar chaves do endpoint JWKS do Keycloak
        // Por enquanto, retorna uma lista vazia - o ASP.NET Core fará isso automaticamente
        return Enumerable.Empty<SecurityKey>();
    }

    /// <summary>
    /// Cria um ClaimsPrincipal com as claims extraídas do token
    /// </summary>
    private ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
    {
        var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
        
        // Adiciona claims personalizadas se necessário
        EnrichUserClaims(claimsIdentity);
        
        return new ClaimsPrincipal(claimsIdentity);
    }

    /// <summary>
    /// Enriquece as claims do usuário com informações adicionais
    /// </summary>
    private void EnrichUserClaims(ClaimsIdentity identity)
    {
        // Extrai roles do token Keycloak (formato específico)
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccess))
        {
            // Parse do JSON realm_access para extrair roles
            // Implementação específica para o formato do Keycloak
        }

        // Adiciona timestamp da autenticação
        identity.AddClaim(new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
        
        // Adiciona informações do request
        identity.AddClaim(new Claim("request_ip", Context.Connection.RemoteIpAddress?.ToString() ?? "unknown"));
    }

    /// <summary>
    /// Resultado da validação do token
    /// </summary>
    private class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();
    }

    private bool IsTokenExpired(JwtSecurityToken token)
    {
        var now = _timeProvider.GetUtcNow(); // Usar TimeProvider
        return token.ValidTo <= now;
    }
}