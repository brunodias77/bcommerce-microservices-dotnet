using ClientService.Application.Services;
using ClientService.Application.UseCases.Clients.Create;
using ClientService.Application.UseCases.Clients.Login;
using ClientService.Application.UseCases.Clients.GetProfile; // Adicionar esta linha
using ClientService.Domain.Repositories;
using ClientService.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization; // Adicionar esta linha
using Microsoft.AspNetCore.Mvc;

namespace ClientService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientController> _logger;
    private readonly ICreateClientUseCase _createClientUseCase;
    private readonly ILoginClientUseCase _loginClientUseCase;
    private readonly IGetClientProfileUseCase _getClientProfileUseCase; // Adicionar esta linha
    private readonly IKeycloakService _keycloakService;

    public ClientController(
        IClientRepository clientRepository, 
        ILogger<ClientController> logger,
        ICreateClientUseCase createClientUseCase,
        ILoginClientUseCase loginClientUseCase,
        IGetClientProfileUseCase getClientProfileUseCase, // Adicionar esta linha
        IKeycloakService keycloakService)
    {
        _clientRepository = clientRepository;
        _logger = logger;
        _createClientUseCase = createClientUseCase;
        _loginClientUseCase = loginClientUseCase;
        _getClientProfileUseCase = getClientProfileUseCase; // Adicionar esta linha
        _keycloakService = keycloakService;
    }

    /// <summary>
    /// Cria um novo cliente no sistema
    /// </summary>
    /// <param name="input">Dados do cliente a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da criação do cliente</returns>
    [HttpPost("create-user")]
    public async Task<IActionResult> CreateClient(
        [FromBody] CreateClientInput input, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando criação de cliente para email: {Email}", input.Email);
            
            var result = await _createClientUseCase.Execute(input);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Cliente criado com sucesso. UserId: {UserId}", result.Value?.UserId);
                
                return CreatedAtAction(
                    nameof(CreateClient),
                    new { id = result.Value?.UserId },
                    result.Value
                );
            }
            else
            {
                _logger.LogWarning("Falha na criação do cliente. Erros: {Errors}", 
                    string.Join(", ", result.Error?.Errors?.Select(e => e.Message) ?? new List<string>()));
                
                return BadRequest(new
                {
                    Message = "Falha na criação do cliente",
                    Errors = result.Error?.Errors?.Select(e => new { e.Code, e.Message }) ?? Enumerable.Empty<object>(),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno durante a criação do cliente");
            
            return StatusCode(500, new
            {
                Message = "Erro interno do servidor",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Endpoint de teste para verificar a conectividade com o banco de dados
    /// </summary>
    /// <returns>Status da conexão com o banco</returns>
    [HttpGet("health-check")]
    public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando health check do banco de dados...");
            
            // Tenta buscar um cliente inexistente para testar a conexão
            var testClient = await _clientRepository.GetByIdAsync(Guid.NewGuid(), cancellationToken);
            
            _logger.LogInformation("Health check concluído com sucesso!");
            
            return Ok(new
            {
                Status = "Healthy",
                Message = "Banco de dados conectado e funcionando corretamente",
                Timestamp = DateTime.UtcNow,
                Database = "PostgreSQL",
                Service = "ClientService"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o health check do banco de dados");
            
            return StatusCode(500, new
            {
                Status = "Unhealthy",
                Message = "Erro ao conectar com o banco de dados",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow,
                Database = "PostgreSQL",
                Service = "ClientService"
            });
        }
    }

    /// <summary>
    /// Endpoint de teste para verificar se existem clientes no banco
    /// </summary>
    /// <returns>Informações sobre clientes no banco</returns>
    [HttpGet("database-info")]
    public async Task<IActionResult> GetDatabaseInfo(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verificando informações do banco de dados...");
            
            // Testa diferentes consultas para verificar o funcionamento
            var testEmailString = "test@example.com";
            var testEmail = Email.Create(testEmailString);
            var emailExists = await _clientRepository.ExistsByEmailAsync(testEmail, cancellationToken);
            
            _logger.LogInformation("Verificação de informações concluída!");
            
            return Ok(new
            {
                Status = "Success",
                Message = "Consultas ao banco executadas com sucesso",
                Tests = new
                {
                    EmailExistsQuery = new
                    {
                        Email = testEmailString,
                        Exists = emailExists,
                        Status = "OK"
                    },
                    DatabaseConnection = "Active",
                    TablesAccessible = "Yes"
                },
                Timestamp = DateTime.UtcNow,
                Database = "PostgreSQL",
                Service = "ClientService"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar informações do banco de dados");
            
            return StatusCode(500, new
            {
                Status = "Error",
                Message = "Erro ao executar consultas no banco de dados",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow,
                Database = "PostgreSQL",
                Service = "ClientService"
            });
        }
    }

    /// <summary>
    /// Endpoint simples para testar se o controller está funcionando
    /// </summary>
    /// <returns>Mensagem de teste</returns>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Status = "Online",
            Message = "ClientController está funcionando!",
            Timestamp = DateTime.UtcNow,
            Service = "ClientService",
            Version = "1.0.0"
        });
    }

    /// <summary>
    /// Endpoint de teste para deletar um usuário do Keycloak
    /// </summary>
    /// <param name="userId">ID do usuário a ser deletado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da operação de deleção</returns>
    [HttpDelete("test-delete-keycloak-user/{userId}")]
    public async Task<IActionResult> TestDeleteKeycloakUser(
        [FromRoute] string userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando teste de deleção de usuário Keycloak. UserId: {UserId}", userId);
            
            // Validar entrada
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("UserId não fornecido para deleção");
                return BadRequest(new
                {
                    Message = "ID do usuário é obrigatório",
                    Timestamp = DateTime.UtcNow
                });
            }

            // Chamar o serviço de deleção
            var result = await _keycloakService.DeleteUserAsync(userId);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Usuário deletado com sucesso do Keycloak. UserId: {UserId}", userId);
                
                return Ok(new
                {
                    Message = "Usuário deletado com sucesso do Keycloak",
                    UserId = userId,
                    Success = result.Value,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Falha na deleção do usuário Keycloak. UserId: {UserId}, Erros: {Errors}", 
                    userId, string.Join(", ", result.Error?.Errors?.Select(e => e.Message) ?? new List<string>()));
                
                return BadRequest(new
                {
                    Message = "Falha na deleção do usuário no Keycloak",
                    UserId = userId,
                    Errors = result.Error?.Errors?.Select(e => new { e.Code, e.Message }) ?? Enumerable.Empty<object>(),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno durante a deleção do usuário Keycloak. UserId: {UserId}", userId);
            
            return StatusCode(500, new
            {
                Message = "Erro interno do servidor",
                UserId = userId,
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Endpoint de teste para obter token de admin do Keycloak
    /// </summary>
    /// <returns>Status da obtenção do token</returns>
    [HttpGet("test-keycloak-admin-token")]
    public async Task<IActionResult> TestKeycloakAdminToken()
    {
        try
        {
            _logger.LogInformation("Testando obtenção de token de admin do Keycloak...");
            
            var token = await _keycloakService.GetAdminTokenAsync();
            
            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogInformation("Token de admin obtido com sucesso");
                
                return Ok(new
                {
                    Message = "Token de admin obtido com sucesso",
                    HasToken = true,
                    TokenLength = token.Length,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Falha ao obter token de admin do Keycloak");
                
                return BadRequest(new
                {
                    Message = "Falha ao obter token de admin do Keycloak",
                    HasToken = false,
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao testar token de admin do Keycloak");
            
            return StatusCode(500, new
            {
                Message = "Erro interno do servidor",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Realiza login de um cliente no sistema
    /// </summary>
    /// <param name="input">Dados de login do cliente</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado do login com tokens de acesso</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginClientInput input, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Iniciando processo de login para email: {Email}", input.Email);
            
            var result = await _loginClientUseCase.Execute(input);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Login realizado com sucesso para email: {Email}", input.Email);
                
                return Ok(new
                {
                    Message = "Login realizado com sucesso",
                    Data = result.Value,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Falha no login para email: {Email}. Erros: {Errors}", 
                    input.Email, string.Join(", ", result.Error?.Errors?.Select(e => e.Message) ?? new List<string>()));
                
                return BadRequest(new
                {
                    Message = "Falha no processo de login",
                    Errors = result.Error?.Errors?.Select(e => new { e.Code, e.Message }) ?? Enumerable.Empty<object>(),
                    Timestamp = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno durante o processo de login para email: {Email}", input.Email);
            
            return StatusCode(500, new
            {
                Message = "Erro interno do servidor",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Obtém o perfil do cliente autenticado
    /// </summary>
    /// <returns>Dados do perfil do cliente</returns>
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetClientProfile()
    {
        try
        {
            _logger.LogInformation("Iniciando obtenção do perfil do cliente");
        
            var result = await _getClientProfileUseCase.Execute();
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Perfil do cliente obtido com sucesso");
                return Ok(result.Value);
            }
        
            _logger.LogWarning("Falha ao obter perfil do cliente. Erros: {Errors}", 
                string.Join(", ", result.Error.Errors.Select(e => e.Message)));
            return NotFound(result.Error.Errors.Select(e => e.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro interno ao obter perfil do cliente");
            return StatusCode(500, "Erro interno do servidor");
        }
    }
}