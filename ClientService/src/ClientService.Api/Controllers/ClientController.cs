using ClientService.Application.UseCases.Clients.Create;
using ClientService.Domain.Repositories;
using ClientService.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace ClientService.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientController> _logger;
    private readonly ICreateClientUseCase _createClientUseCase;

    public ClientController(
        IClientRepository clientRepository, 
        ILogger<ClientController> logger,
        ICreateClientUseCase createClientUseCase)
    {
        _clientRepository = clientRepository;
        _logger = logger;
        _createClientUseCase = createClientUseCase;
    }

    /// <summary>
    /// Cria um novo cliente no sistema
    /// </summary>
    /// <param name="input">Dados do cliente a ser criado</param>
    /// <param name="cancellationToken">Token de cancelamento</param>
    /// <returns>Resultado da criação do cliente</returns>
    [HttpPost]
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
}