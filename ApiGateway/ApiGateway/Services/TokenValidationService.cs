using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace ApiGateway.Services;

public class TokenValidationService : ITokenValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenValidationService> _logger;

    public TokenValidationService(IConfiguration configuration, ILogger<TokenValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var tokenHandler = new JwtSecurityTokenHandler();
            var keycloakAuthority = _configuration["Keycloak:Authority"];
            var audience = _configuration["Keycloak:Audience"];

            if (string.IsNullOrEmpty(keycloakAuthority) || string.IsNullOrEmpty(audience))
            {
                _logger.LogError("Configurações do Keycloak não encontradas");
                return false;
            }

            // Para validação local, vamos verificar se o token é válido
            // Em produção, você deve validar contra o Keycloak
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = false, // Desabilitado para desenvolvimento
                ValidIssuer = keycloakAuthority,
                ValidAudience = audience,
                ClockSkew = TimeSpan.Zero
            };

            var principal = await Task.Run(() => tokenHandler.ValidateToken(token, validationParameters, out _));
            return principal?.Identity?.IsAuthenticated == true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token");
            return false;
        }
    }

    public async Task<IDictionary<string, object>> GetTokenClaimsAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = await Task.Run(() => tokenHandler.ReadJwtToken(token));
            
            return jwtToken.Claims.ToDictionary(
                claim => claim.Type,
                claim => (object)claim.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao extrair claims do token");
            return new Dictionary<string, object>();
        }
    }
} 