using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GatewayController : ControllerBase
{
    private readonly ITokenValidationService _tokenValidationService;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(
        ITokenValidationService tokenValidationService,
        ILogger<GatewayController> logger)
    {
        _tokenValidationService = tokenValidationService;
        _logger = logger;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "B-Commerce API Gateway",
            Version = "1.0.0"
        });
    }

    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult Info()
    {
        return Ok(new
        {
            Service = "B-Commerce API Gateway",
            Description = "API Gateway para o sistema B-Commerce usando YARP",
            Version = "1.0.0",
            Technologies = new[]
            {
                ".NET 8",
                "YARP (Yet Another Reverse Proxy)",
                "JWT Authentication",
                "Keycloak Integration"
            },
            Routes = new[]
            {
                "/api/client/* - Client Service (autenticado)",
                "/api/client/create-user - Criar usuário (público)",
                "/api/client/login - Login (público)"
            }
        });
    }

    [HttpPost("validate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest(new { Error = "Token é obrigatório" });
            }

            var isValid = await _tokenValidationService.ValidateTokenAsync(request.Token);
            
            if (isValid)
            {
                var claims = await _tokenValidationService.GetTokenClaimsAsync(request.Token);
                return Ok(new
                {
                    Valid = true,
                    Claims = claims
                });
            }

            return Ok(new { Valid = false });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token");
            return StatusCode(500, new { Error = "Erro interno do servidor" });
        }
    }
}

public class TokenValidationRequest
{
    public string Token { get; set; } = string.Empty;
} 