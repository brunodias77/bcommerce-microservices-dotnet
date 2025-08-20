namespace ApiGateway.Services;

public interface ITokenValidationService
{
    Task<bool> ValidateTokenAsync(string token);
    Task<IDictionary<string, object>> GetTokenClaimsAsync(string token);
} 