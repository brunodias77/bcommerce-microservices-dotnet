using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Services;
using ClientService.Infra.Data;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Infra.Services;

public class LoggedUser : ILoggedUser
{
    private readonly ClientDbContext _dbContext;
    private readonly ITokenProvider _tokenProvider;

    public LoggedUser(ClientDbContext dbContext, ITokenProvider tokenProvider)
    {
        _dbContext = dbContext;
        _tokenProvider = tokenProvider;
    }

    public async Task<Client?> User()
    {
        try
        {
            // Obtém o valor do token atual
            var token = _tokenProvider.Value();
            if (string.IsNullOrEmpty(token))
                return null;

            // Cria um manipulador para o token JWT
            var tokenHandler = new JwtSecurityTokenHandler();

            // Lê o token JWT
            var jwtSecurityToken = tokenHandler.ReadJwtToken(token);

            // Obtém o valor do claim 'sub' (subject) do token JWT
            var subjectClaim = jwtSecurityToken.Claims.FirstOrDefault(c => c.Type == "sub");
            if (subjectClaim == null || string.IsNullOrEmpty(subjectClaim.Value))
                return null;

            // Converte o identificador do usuário para um Guid
            if (!Guid.TryParse(subjectClaim.Value, out var keycloakUserId))
                return null;

            // Busca o usuário no banco de dados pelo KeycloakUserId
            return await _dbContext.Clients
                .AsNoTracking()
                .FirstOrDefaultAsync(client => 
                    client.KeycloakUserId == keycloakUserId &&
                    client.Status == Domain.Enums.ClientStatus.Ativo);
        }
        catch
        {
            return null;
        }
    }
}