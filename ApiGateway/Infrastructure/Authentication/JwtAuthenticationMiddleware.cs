using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace ApiGateway.Infrastructure.Authentication;

/// <summary>
/// Middleware personalizado para autenticação JWT com funcionalidades avançadas
/// Gerencia bypass de endpoints públicos, refresh de tokens e extração de contexto
/// </summary>
public class JwtAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtAuthenticationMiddleware> _logger;
    private readonly KeycloakAuthenticationOptions _options;
    private readonly IServiceProvider _serviceProvider;

    public JwtAuthenticationMiddleware(
        RequestDelegate next,
        ILogger<JwtAuthenticationMiddleware> logger,
        IOptions<KeycloakAuthenticationOptions> options,
        IServiceProvider serviceProvider)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Processa a requisição HTTP aplicando lógica de autenticação personalizada
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            if (IsPublicEndpoint(context.Request.Path))
            {
                _logger.LogDebug("Public endpoint {Path} accessed, skipping authentication",
                    context.Request.Path);
                await _next(context);
                return;
            }

            var authResult = await ProcessAuthenticationAsync(context);

            if (authResult.Succeeded) // CORRETO: usar propriedade, não método
            {
                context.User = authResult.Principal;

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["UserId"] = authResult.Principal.FindFirst("sub")?.Value ?? "unknown",
                    ["UserName"] = authResult.Principal.FindFirst("preferred_username")?.Value ?? "unknown"
                }))
                {
                    await _next(context);
                }
            }
            else
            {
                await HandleAuthenticationFailureAsync(context, authResult.Failure);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in JWT authentication middleware");
            await HandleAuthenticationFailureAsync(context, "Internal authentication error");
        }
    }
    /// <summary>
    /// Verifica se o endpoint é público (não requer autenticação)
    /// </summary>
    private bool IsPublicEndpoint(PathString path)
    {
        var pathValue = path.Value?.ToLowerInvariant();
        if (string.IsNullOrEmpty(pathValue))
            return false;

        return _options.PublicEndpoints.Any(endpoint =>
            pathValue.StartsWith(endpoint.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Processa a autenticação JWT da requisição
    /// </summary>
    private async Task<AuthenticationResult> ProcessAuthenticationAsync(HttpContext context)
    {
        var token = ExtractTokenFromRequest(context.Request);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("No JWT token found in request to protected endpoint {Path}",
                context.Request.Path);
            return AuthenticationResult.Failed("No JWT token provided");
        }

        var validationResult = await ValidateTokenAsync(token);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning("JWT token validation failed: {Error}", validationResult.Error);

            // CORRIGIDO - usar propriedade correta
            if (validationResult.IsExpired && _options.TokenRefresh.EnableAutoRefresh)
            {
                var refreshResult = await TryRefreshTokenAsync(context, token);
                if (refreshResult.Succeeded) // CORRIGIDO - usar propriedade
                {
                    return refreshResult;
                }
            }

            return AuthenticationResult.Failed(validationResult.Error ?? "Token validation failed");
        }

        var principal = CreateClaimsPrincipal(validationResult.Claims, context);
        return AuthenticationResult.Success(principal); // CORRIGIDO - usar método estático
    }



    /// <summary>
    /// Extrai o token JWT da requisição
    /// </summary>
    private string? ExtractTokenFromRequest(HttpRequest request)
    {
        // Verifica header Authorization
        var authHeader = request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        // Verifica cookie (fallback)
        var tokenCookie = request.Cookies["access_token"];
        if (!string.IsNullOrEmpty(tokenCookie))
        {
            return tokenCookie;
        }

        // Verifica query parameter (apenas para desenvolvimento)
        if (request.Query.ContainsKey("access_token"))
        {
            _logger.LogWarning("JWT token provided via query parameter - not recommended for production");
            return request.Query["access_token"].FirstOrDefault();
        }

        return null;
    }

    /// <summary>
    /// Valida o token JWT
    /// </summary>
    private async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Lê o token para verificar expiração antes da validação completa
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var isExpired = jwtToken.ValidTo < DateTime.UtcNow;

            // Usa o serviço de autenticação do ASP.NET Core para validação
            using var scope = _serviceProvider.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

            var result = await authService.AuthenticateAsync(
                new DefaultHttpContext { Request = { Headers = { ["Authorization"] = $"Bearer {token}" } } },
                "Bearer");

            if (result.Succeeded)
            {
                return new TokenValidationResult
                {
                    IsValid = true,
                    Claims = result.Principal.Claims,
                    IsExpired = false
                };
            }
            else
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = result.Failure?.Message ?? "Token validation failed",
                    IsExpired = isExpired
                };
            }
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult
            {
                IsValid = false,
                Error = "Token expired",
                IsExpired = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating JWT token");
            return new TokenValidationResult
            {
                IsValid = false,
                Error = ex.Message,
                IsExpired = false
            };
        }
    }

    /// <summary>
    /// Tenta fazer refresh do token expirado
    /// </summary>
    private async Task<AuthenticationResult> TryRefreshTokenAsync(HttpContext context, string expiredToken)
    {
        try
        {
            _logger.LogInformation("Attempting to refresh expired JWT token");

            // Extrai refresh token do cookie ou header
            var refreshToken = context.Request.Cookies["refresh_token"] ??
                              context.Request.Headers["X-Refresh-Token"].FirstOrDefault();

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("No refresh token available for token refresh");
                return AuthenticationResult.Failed("Token expired and no refresh token available");
            }

            // Chama o endpoint de refresh do Keycloak
            var newTokens = await RefreshTokenWithKeycloakAsync(refreshToken);
            if (newTokens != null)
            {
                // Atualiza os cookies com os novos tokens
                UpdateTokenCookies(context.Response, newTokens);

                // Valida o novo token
                var validationResult = await ValidateTokenAsync(newTokens.AccessToken);
                if (validationResult.IsValid)
                {
                    var principal = CreateClaimsPrincipal(validationResult.Claims, context);
                    _logger.LogInformation("Token refresh successful");
                    return AuthenticationResult.Success(principal); // CORRIGIDO: usar método estático
                }
            }

            _logger.LogWarning("Token refresh failed");
            return AuthenticationResult.Failed("Token refresh failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return AuthenticationResult.Failed("Token refresh error");
        }
    }


    /// <summary>
    /// Faz refresh do token com o Keycloak
    /// </summary>
    private async Task<TokenResponse?> RefreshTokenWithKeycloakAsync(string refreshToken)
    {
        // CORRIGIDO - usar propriedade correta
        if (string.IsNullOrEmpty(_options.TokenRefresh.RefreshEndpoint) ||
            string.IsNullOrEmpty(_options.TokenRefresh.ClientId))
        {
            return null;
        }

        using var httpClient = new HttpClient();

        var requestData = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = _options.TokenRefresh.ClientId
        };

        if (!string.IsNullOrEmpty(_options.TokenRefresh.ClientSecret))
        {
            requestData["client_secret"] = _options.TokenRefresh.ClientSecret;
        }

        var requestContent = new FormUrlEncodedContent(requestData);

        // CORRIGIDO - usar propriedade correta
        var response = await httpClient.PostAsync(_options.TokenRefresh.RefreshEndpoint, requestContent);

        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
        }

        return null;
    }

    /// <summary>
    /// Atualiza os cookies com os novos tokens
    /// </summary>
    private void UpdateTokenCookies(HttpResponse response, TokenResponse tokens)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddSeconds(tokens.ExpiresIn)
        };

        response.Cookies.Append("access_token", tokens.AccessToken, cookieOptions);

        if (!string.IsNullOrEmpty(tokens.RefreshToken))
        {
            var refreshCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30) // Refresh token geralmente tem vida longa
            };

            response.Cookies.Append("refresh_token", tokens.RefreshToken, refreshCookieOptions);
        }
    }

    /// <summary>
    /// Cria um ClaimsPrincipal com as claims do token
    /// </summary>
    private ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims, HttpContext context)
    {
        var claimsIdentity = new ClaimsIdentity(claims, "Bearer");

        // Adiciona claims de contexto
        claimsIdentity.AddClaim(new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
        claimsIdentity.AddClaim(new Claim("request_ip", context.Connection.RemoteIpAddress?.ToString() ?? "unknown"));
        claimsIdentity.AddClaim(new Claim("user_agent", context.Request.Headers["User-Agent"].FirstOrDefault() ?? "unknown"));

        return new ClaimsPrincipal(claimsIdentity);
    }

    /// <summary>
    /// Trata falhas de autenticação
    /// </summary>
    private async Task HandleAuthenticationFailureAsync(HttpContext context, string? error)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = "unauthorized",
            message = "Authentication failed",
            details = error ?? "Unknown error", // CORRIGIDO - null check
            timestamp = DateTimeOffset.UtcNow
        };

        var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }

    /// <summary>
    /// Resultado da validação do token
    /// </summary>
    private class TokenValidationResult
    {
        public bool IsValid { get; set; }
        public string? Error { get; set; }
        public bool IsExpired { get; set; }
        public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();
    }

    /// <summary>
    /// Resultado da autenticação
    /// </summary>
    private class AuthenticationResult
    {
        public bool Succeeded { get; private set; }
        public ClaimsPrincipal Principal { get; private set; } = new();
        public string? Failure { get; private set; }

        // Construtor privado para forçar uso dos métodos estáticos
        private AuthenticationResult() { }

        public static AuthenticationResult Success(ClaimsPrincipal principal)
        {
            return new AuthenticationResult
            {
                Succeeded = true,
                Principal = principal
            };
        }

        public static AuthenticationResult Failed(string error)
        {
            return new AuthenticationResult
            {
                Succeeded = false,
                Failure = error ?? "Authentication failed"
            };
        }
    }


    /// <summary>
    /// Resposta do endpoint de refresh de token
    /// </summary>
    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}