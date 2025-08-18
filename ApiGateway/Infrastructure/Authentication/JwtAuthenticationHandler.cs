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
/// 🔐 CLASSE DE AUTENTICAÇÃO JWT PERSONALIZADA
/// 
/// Esta classe é um "Handler" (manipulador) personalizado para autenticação JWT.
/// Ela herda do JwtBearerHandler do ASP.NET Core e adiciona funcionalidades extras.
/// 
/// O QUE FAZ:
/// - Valida tokens JWT vindos do Keycloak
/// - Extrai informações do usuário do token
/// - Verifica se o endpoint é público ou privado
/// - Trata erros de token expirado ou inválido
/// 
/// CONCEITOS IMPORTANTES:
/// - JWT = JSON Web Token (token de segurança)
/// - Handler = classe que processa um tipo específico de autenticação
/// - Claims = informações sobre o usuário (ID, nome, roles, etc.)
/// - Principal = representação do usuário autenticado no sistema
/// </summary>
public class JwtAuthenticationHandler : JwtBearerHandler
{
    // 📋 DEPENDÊNCIAS INJETADAS
    // Essas dependências são fornecidas pelo sistema de DI (Dependency Injection) do .NET
    private readonly ILogger<JwtAuthenticationHandler> _logger;           // Para registrar logs/eventos
    private readonly KeycloakAuthenticationOptions _options;              // Configurações do Keycloak
    private readonly HttpClient _httpClient;                              // Para fazer chamadas HTTP (se necessário)
    private readonly TimeProvider _timeProvider;                          // Para obter data/hora atual

    /// <summary>
    /// 🏗️ CONSTRUTOR - RECEBE TODAS AS DEPENDÊNCIAS
    /// 
    /// O construtor é chamado automaticamente pelo sistema de DI do ASP.NET Core.
    /// Todas essas dependências são registradas no Program.cs e injetadas automaticamente.
    /// 
    /// PARÂMETROS:
    /// - jwtOptions: Configurações padrão do JWT Bearer
    /// - loggerFactory: Fábrica para criar loggers
    /// - encoder: Para codificar URLs (necessário para o handler base)
    /// - keycloakOptions: Nossas configurações personalizadas do Keycloak
    /// - httpClient: Para fazer chamadas HTTP externas
    /// - timeProvider: Para obter data/hora de forma testável
    /// </summary>
    public JwtAuthenticationHandler(
        IOptionsMonitor<JwtBearerOptions> jwtOptions,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        IOptions<KeycloakAuthenticationOptions> keycloakOptions,
        HttpClient httpClient,
        TimeProvider timeProvider)
        : base(jwtOptions, loggerFactory, encoder) // Chama o construtor da classe pai (JwtBearerHandler)
    {
        // Inicializa as variáveis privadas com as dependências recebidas
        _logger = loggerFactory.CreateLogger<JwtAuthenticationHandler>();
        _options = keycloakOptions.Value; // .Value extrai a configuração do Options pattern
        _httpClient = httpClient;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// 🚀 MÉTODO PRINCIPAL DE AUTENTICAÇÃO
    /// 
    /// Este método é chamado AUTOMATICAMENTE pelo ASP.NET Core para cada requisição HTTP.
    /// É um override (sobrescrita) do método da classe pai.
    /// 
    /// FLUXO DE EXECUÇÃO:
    /// 1. Verifica se o endpoint é público (não precisa de autenticação)
    /// 2. Extrai o token JWT do header Authorization
    /// 3. Valida o token
    /// 4. Cria um Principal (usuário autenticado)
    /// 5. Retorna sucesso ou falha
    /// 
    /// RETORNO:
    /// - AuthenticateResult.Success: Usuário autenticado com sucesso
    /// - AuthenticateResult.Fail: Autenticação falhou
    /// - AuthenticateResult.NoResult: Endpoint público, pula autenticação
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        try
        {
            // 🔍 PASSO 1: Verifica se é um endpoint público
            // Endpoints públicos não precisam de token (ex: /health, /login)
            if (IsPublicEndpoint())
            {
                _logger.LogDebug("Public endpoint accessed, skipping authentication");
                return AuthenticateResult.NoResult(); // NoResult = "não fazer nada, deixa passar"
            }

            // 🔍 PASSO 2: Extrai o token do header "Authorization: Bearer xyz123"
            var token = ExtractTokenFromHeader();
            if (string.IsNullOrEmpty(token))
            {
                // Se não tem token, registra um warning e retorna falha
                _logger.LogWarning("No JWT token found in Authorization header");
                return AuthenticateResult.Fail("No JWT token provided");
            }

            // 🔍 PASSO 3: Valida o token JWT
            // Verifica assinatura, expiração, issuer, audience, etc.
            var validationResult = await ValidateTokenAsync(token);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("JWT token validation failed: {Error}", validationResult.Error);
                return AuthenticateResult.Fail(validationResult.Error ?? "Token validation failed");
            }

            // 🔍 PASSO 4: Cria o Principal (usuário autenticado)
            // Principal é como o sistema representa o usuário logado
            var principal = CreateClaimsPrincipal(validationResult.Claims);
            
            // Cria um "ticket" de autenticação (comprovante de que o usuário é válido)
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            // Log de sucesso com o ID do usuário
            _logger.LogDebug("JWT authentication successful for user: {UserId}",
                principal.FindFirst("sub")?.Value); // "sub" = subject = ID do usuário

            // 🎉 Retorna sucesso!
            return AuthenticateResult.Success(ticket);
        }
        // 🚨 TRATAMENTO DE ERROS ESPECÍFICOS
        catch (SecurityTokenExpiredException ex)
        {
            // Token expirado - erro específico do JWT
            _logger.LogWarning("JWT token expired: {Message}", ex.Message);
            return AuthenticateResult.Fail("Token expired");
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            // Assinatura do token inválida - possível tentativa de falsificação
            _logger.LogWarning("JWT token has invalid signature: {Message}", ex.Message);
            return AuthenticateResult.Fail("Invalid token signature");
        }
        catch (Exception ex)
        {
            // Qualquer outro erro inesperado
            _logger.LogError(ex, "Unexpected error during JWT authentication");
            return AuthenticateResult.Fail("Authentication error");
        }
    }

    /// <summary>
    /// 🔓 VERIFICA SE O ENDPOINT É PÚBLICO
    /// 
    /// Alguns endpoints não precisam de autenticação:
    /// - /health (verificação de saúde do sistema)
    /// - /login (para fazer login)
    /// - /api/catalog/products (catálogo público)
    /// 
    /// COMO FUNCIONA:
    /// 1. Pega o caminho da URL atual
    /// 2. Compara com uma lista de caminhos públicos
    /// 3. Retorna true se encontrar uma correspondência
    /// </summary>
    private bool IsPublicEndpoint()
    {
        // Pega o caminho da URL atual e converte para minúsculas
        // Ex: "/api/clients/login" vira "/api/clients/login"
        var path = Context.Request.Path.Value?.ToLowerInvariant();

        // Lista de endpoints que NÃO precisam de autenticação
        var publicEndpoints = new[]
        {
            "/health",                    // Health check do sistema
            "/health/ready",              // Verifica se está pronto
            "/health/live",               // Verifica se está vivo
            "/metrics",                   // Métricas do Prometheus
            "/api/clients/create-user",   // Criação de usuário
            "/api/clients/login",         // Login
            "/api/catalog/products",      // Catálogo público de produtos
            "/swagger",                   // Documentação da API
            "/api-docs"                   // Documentação da API
        };

        // Verifica se o caminho atual começa com algum endpoint público
        // Any() = "existe algum elemento que atenda a condição?"
        return publicEndpoints.Any(endpoint => path?.StartsWith(endpoint) == true);
    }

    /// <summary>
    /// 🔍 EXTRAI O TOKEN JWT DO HEADER AUTHORIZATION
    /// 
    /// O token JWT vem no formato: "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    /// 
    /// PASSOS:
    /// 1. Pega o valor do header "Authorization"
    /// 2. Verifica se começa com "Bearer "
    /// 3. Remove o "Bearer " e retorna só o token
    /// 
    /// EXEMPLO:
    /// Input:  "Bearer abc123"
    /// Output: "abc123"
    /// </summary>
    private string? ExtractTokenFromHeader()
    {
        // Pega o primeiro valor do header "Authorization"
        // Headers podem ter múltiplos valores, mas geralmente é só um
        var authHeader = Context.Request.Headers["Authorization"].FirstOrDefault();

        // Se não tem header ou não começa com "Bearer ", retorna null
        if (string.IsNullOrEmpty(authHeader) || 
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Remove "Bearer " do início e retorna só o token
        // "Bearer abc123".Substring("Bearer ".Length) = "abc123"
        return authHeader.Substring("Bearer ".Length).Trim();
    }

    /// <summary>
    /// ✅ VALIDA O TOKEN JWT
    /// 
    /// Esta é a parte mais importante! Aqui verificamos se o token é válido:
    /// - Assinatura (não foi alterado)
    /// - Expiração (ainda está válido)
    /// - Issuer (quem criou o token)
    /// - Audience (para quem o token foi criado)
    /// 
    /// PROCESSO:
    /// 1. Configura os parâmetros de validação
    /// 2. Usa o JwtSecurityTokenHandler para validar
    /// 3. Extrai as claims (informações do usuário) se válido
    /// 4. Retorna resultado com sucesso/falha
    /// </summary>
    private async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            // Cria o handler do .NET para trabalhar com JWT
            var tokenHandler = new JwtSecurityTokenHandler();

            // 🔧 CONFIGURA OS PARÂMETROS DE VALIDAÇÃO
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,              // Verifica quem criou o token (Keycloak)
                ValidateAudience = true,            // Verifica se o token é para nossa aplicação
                ValidateLifetime = true,            // Verifica se o token não expirou
                ValidateIssuerSigningKey = true,    // Verifica a assinatura digital
                
                // Valores esperados (configurados no appsettings.json)
                ValidIssuer = _options.Authority,           // URL do Keycloak
                ValidAudiences = _options.ValidAudiences,   // IDs das aplicações válidas
                
                // Tolerância para diferenças de relógio entre servidores
                ClockSkew = TimeSpan.FromSeconds(_options.ClockSkewSeconds),
                
                // Função para obter chaves de assinatura do Keycloak
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                {
                    return GetKeycloakSigningKeys();
                }
            };

            // 🔍 EXECUTA A VALIDAÇÃO
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);

            if (result.IsValid)
            {
                // ✅ TOKEN VÁLIDO
                var jwtToken = result.SecurityToken as JwtSecurityToken;
                return new TokenValidationResult
                {
                    IsValid = true,
                    Claims = result.ClaimsIdentity.Claims.ToList(), // Informações do usuário
                    Token = jwtToken // Token decodificado
                };
            }
            else
            {
                // ❌ TOKEN INVÁLIDO
                return new TokenValidationResult
                {
                    IsValid = false,
                    Error = result.Exception?.Message ?? "Token validation failed"
                };
            }
        }
        catch (Exception ex)
        {
            // 🚨 ERRO INESPERADO
            _logger.LogError(ex, "Error validating JWT token");
            return new TokenValidationResult
            {
                IsValid = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// 🔑 OBTÉM AS CHAVES DE ASSINATURA DO KEYCLOAK
    /// 
    /// O Keycloak usa chaves criptográficas para assinar os tokens JWT.
    /// Para validar um token, precisamos da chave pública correspondente.
    /// 
    /// NOTA: Esta implementação está simplificada.
    /// Em produção, você buscaria as chaves do endpoint JWKS do Keycloak:
    /// https://seu-keycloak.com/realms/seu-realm/.well-known/jwks_uri
    /// 
    /// Por enquanto, retorna vazio e deixa o ASP.NET Core fazer automaticamente.
    /// </summary>
    private IEnumerable<SecurityKey> GetKeycloakSigningKeys()
    {
        // TODO: Implementar busca das chaves JWKS do Keycloak
        // Esta implementação seria expandida para buscar chaves do endpoint JWKS do Keycloak
        // Por enquanto, retorna uma lista vazia - o ASP.NET Core fará isso automaticamente
        return Enumerable.Empty<SecurityKey>();
    }

    /// <summary>
    /// 👤 CRIA UM CLAIMSPRINCIPAL (USUÁRIO AUTENTICADO)
    /// 
    /// ClaimsPrincipal é como o .NET representa um usuário autenticado.
    /// Contém todas as informações sobre o usuário (claims).
    /// 
    /// CLAIMS são pares chave-valor com informações do usuário:
    /// - "sub" = ID do usuário
    /// - "name" = Nome do usuário
    /// - "email" = Email do usuário
    /// - "roles" = Roles/funções do usuário
    /// 
    /// PROCESSO:
    /// 1. Cria uma ClaimsIdentity com as claims do token
    /// 2. Adiciona claims extras (timestamp, IP, etc.)
    /// 3. Cria o ClaimsPrincipal final
    /// </summary>
    private ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
    {
        // Cria uma identidade com as claims extraídas do token
        // Scheme.Name identifica qual tipo de autenticação foi usado
        var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);

        // Adiciona informações extras se necessário
        EnrichUserClaims(claimsIdentity);

        // Cria e retorna o principal (usuário autenticado)
        return new ClaimsPrincipal(claimsIdentity);
    }

    /// <summary>
    /// 🎨 ENRIQUECE AS CLAIMS COM INFORMAÇÕES EXTRAS
    /// 
    /// Além das claims que vêm no token JWT, podemos adicionar informações extras:
    /// - Timestamp de quando foi autenticado
    /// - IP do usuário
    /// - User Agent do browser
    /// - Roles extraídas do formato específico do Keycloak
    /// 
    /// Isso é útil para auditoria e logs detalhados.
    /// </summary>
    private void EnrichUserClaims(ClaimsIdentity identity)
    {
        // 🔍 EXTRAI ROLES DO TOKEN KEYCLOAK
        // O Keycloak armazena roles em um formato JSON específico
        var realmAccess = identity.FindFirst("realm_access")?.Value;
        if (!string.IsNullOrEmpty(realmAccess))
        {
            // TODO: Parse do JSON realm_access para extrair roles
            // Formato típico: {"roles": ["admin", "user"]}
            // Implementação específica para o formato do Keycloak
        }

        // ⏰ ADICIONA TIMESTAMP DA AUTENTICAÇÃO
        // Útil para saber quando o usuário foi autenticado
        identity.AddClaim(new Claim("auth_time", 
            DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));

        // 🌐 ADICIONA IP DO USUÁRIO
        // Útil para auditoria e segurança
        identity.AddClaim(new Claim("request_ip", 
            Context.Connection.RemoteIpAddress?.ToString() ?? "unknown"));
    }

    /// <summary>
    /// 📊 CLASSE PARA RESULTADO DA VALIDAÇÃO DO TOKEN
    /// 
    /// Esta classe interna (private) encapsula o resultado da validação JWT.
    /// É como um "envelope" que carrega as informações da validação.
    /// 
    /// PROPRIEDADES:
    /// - IsValid: Se o token é válido ou não
    /// - Error: Mensagem de erro se inválido
    /// - Claims: Informações do usuário extraídas do token
    /// - Token: Token JWT decodificado (objeto)
    /// </summary>
    private class TokenValidationResult
    {
        public bool IsValid { get; set; }                                    // Token válido?
        public string? Error { get; set; }                                   // Mensagem de erro
        public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>(); // Claims do usuário
        public JwtSecurityToken? Token { get; set; }                         // Token decodificado
    }

    /// <summary>
    /// ⏰ VERIFICA SE O TOKEN EXPIROU
    /// 
    /// Método utilitário para verificar se um token JWT expirou.
    /// Compara a data de expiração do token com a data/hora atual.
    /// 
    /// PARÂMETROS:
    /// - token: Token JWT decodificado
    /// 
    /// RETORNO:
    /// - true: Token expirou
    /// - false: Token ainda é válido
    /// 
    /// NOTA: Usa TimeProvider para facilitar testes unitários
    /// (permite "mockar" a data/hora atual nos testes)
    /// </summary>
    private bool IsTokenExpired(JwtSecurityToken token)
    {
        // Obtém a data/hora atual usando o TimeProvider
        var now = _timeProvider.GetUtcNow();
        
        // Compara com a data de expiração do token
        // ValidTo = data/hora que o token expira
        return token.ValidTo <= now;
    }
}

/*
📚 CONCEITOS IMPORTANTES PARA ESTUDAR:

1. **JWT (JSON Web Token)**:
   - Formato de token de segurança
   - Contém informações codificadas em Base64
   - Tem 3 partes: Header.Payload.Signature

2. **Claims**:
   - Informações sobre o usuário
   - Formato chave-valor
   - Ex: {"sub": "12345", "name": "João"}

3. **Handler Pattern**:
   - Classe especializada em processar um tipo de operação
   - No ASP.NET Core, handlers processam diferentes tipos de autenticação

4. **Dependency Injection (DI)**:
   - Padrão onde as dependências são fornecidas automaticamente
   - Facilita testes e manutenção do código

5. **Options Pattern**:
   - Padrão para configurações no .NET
   - Permite carregar configurações do appsettings.json

6. **Async/Await**:
   - Programação assíncrona em C#
   - Permite operações não-bloqueantes

📖 PRÓXIMOS PASSOS DE ESTUDO:
- Entender como funciona o JWT
- Estudar o padrão Options no .NET
- Aprender sobre Dependency Injection
- Praticar programação assíncrona com async/await
*/

// using Microsoft.AspNetCore.Authentication;
// using Microsoft.AspNetCore.Authentication.JwtBearer;
// using Microsoft.Extensions.Options;
// using System.IdentityModel.Tokens.Jwt;
// using System.Security.Claims;
// using System.Text.Encodings.Web;
// using Microsoft.IdentityModel.Tokens;
// using Serilog;
//
// namespace ApiGateway.Infrastructure.Authentication;
//
// /// <summary>
// /// Handler personalizado para autenticação JWT com funcionalidades avançadas
// /// Implementa validação customizada, extração de contexto do usuário e tratamento de erros
// /// </summary>
// public class JwtAuthenticationHandler : JwtBearerHandler
// {
//     private readonly ILogger<JwtAuthenticationHandler> _logger;
//     private readonly KeycloakAuthenticationOptions _options;
//     private readonly HttpClient _httpClient;
//     private readonly TimeProvider _timeProvider;
//
//     public JwtAuthenticationHandler(
//         IOptionsMonitor<JwtBearerOptions> jwtOptions,
//         ILoggerFactory loggerFactory,
//         UrlEncoder encoder,
//         IOptions<KeycloakAuthenticationOptions> keycloakOptions,
//         HttpClient httpClient,
//         TimeProvider timeProvider)
//         : base(jwtOptions, loggerFactory, encoder)
//     {
//         _logger = loggerFactory.CreateLogger<JwtAuthenticationHandler>();
//         _options = keycloakOptions.Value;
//         _httpClient = httpClient;
//         _timeProvider = timeProvider;
//     }
//
//     /// <summary>
//     /// Processa a autenticação JWT com lógica personalizada
//     /// </summary>
//     protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
//     {
//         try
//         {
//             // Verifica se o endpoint é público (não requer autenticação)
//             if (IsPublicEndpoint())
//             {
//                 _logger.LogDebug("Public endpoint accessed, skipping authentication");
//                 return AuthenticateResult.NoResult();
//             }
//
//             // Extrai o token do header Authorization
//             var token = ExtractTokenFromHeader();
//             if (string.IsNullOrEmpty(token))
//             {
//                 _logger.LogWarning("No JWT token found in Authorization header");
//                 return AuthenticateResult.Fail("No JWT token provided");
//             }
//
//             var validationResult = await ValidateTokenAsync(token);
//             if (!validationResult.IsValid)
//             {
//                 _logger.LogWarning("JWT token validation failed: {Error}", validationResult.Error);
//                 return AuthenticateResult.Fail(validationResult.Error ?? "Token validation failed"); // CORRIGIDO
//             }
//
//             // Cria o principal com as claims do usuário
//             var principal = CreateClaimsPrincipal(validationResult.Claims);
//             var ticket = new AuthenticationTicket(principal, Scheme.Name);
//
//             _logger.LogDebug("JWT authentication successful for user: {UserId}",
//                 principal.FindFirst("sub")?.Value);
//
//             return AuthenticateResult.Success(ticket);
//         }
//         catch (SecurityTokenExpiredException ex)
//         {
//             _logger.LogWarning("JWT token expired: {Message}", ex.Message);
//             return AuthenticateResult.Fail("Token expired");
//         }
//         catch (SecurityTokenInvalidSignatureException ex)
//         {
//             _logger.LogWarning("JWT token has invalid signature: {Message}", ex.Message);
//             return AuthenticateResult.Fail("Invalid token signature");
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Unexpected error during JWT authentication");
//             return AuthenticateResult.Fail("Authentication error");
//         }
//     }
//
//     /// <summary>
//     /// Verifica se o endpoint atual é público (não requer autenticação)
//     /// </summary>
//     private bool IsPublicEndpoint()
//     {
//         var path = Context.Request.Path.Value?.ToLowerInvariant();
//
//         var publicEndpoints = new[]
//         {
//             "/health",
//             "/health/ready",
//             "/health/live",
//             "/metrics",
//             "/api/clients/create-user",
//             "/api/clients/login",
//             "/api/catalog/products", // Catálogo público
//             "/swagger",
//             "/api-docs"
//         };
//
//         return publicEndpoints.Any(endpoint => path?.StartsWith(endpoint) == true);
//     }
//
//     /// <summary>
//     /// Extrai o token JWT do header Authorization
//     /// </summary>
//     private string? ExtractTokenFromHeader()
//     {
//         var authHeader = Context.Request.Headers["Authorization"].FirstOrDefault();
//
//         if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
//         {
//             return null;
//         }
//
//         return authHeader.Substring("Bearer ".Length).Trim();
//     }
//
//     /// <summary>
//     /// Valida o token JWT usando as configurações do Keycloak
//     /// </summary>
//
//     private async Task<TokenValidationResult> ValidateTokenAsync(string token)
//     {
//         try
//         {
//             var tokenHandler = new JwtSecurityTokenHandler();
//
//             var validationParameters = new TokenValidationParameters
//             {
//                 ValidateIssuer = true,
//                 ValidateAudience = true,
//                 ValidateLifetime = true,
//                 ValidateIssuerSigningKey = true,
//                 ValidIssuer = _options.Authority,
//                 ValidAudiences = _options.ValidAudiences,
//                 ClockSkew = TimeSpan.FromSeconds(_options.ClockSkewSeconds),
//                 IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
//                 {
//                     return GetKeycloakSigningKeys();
//                 }
//             };
//
//             var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
//
//             if (result.IsValid)
//             {
//                 var jwtToken = result.SecurityToken as JwtSecurityToken;
//                 return new TokenValidationResult
//                 {
//                     IsValid = true,
//                     Claims = result.ClaimsIdentity.Claims.ToList(),
//                     Token = jwtToken // CORRIGIDO - agora a propriedade existe
//                 };
//             }
//             else
//             {
//                 return new TokenValidationResult
//                 {
//                     IsValid = false,
//                     Error = result.Exception?.Message ?? "Token validation failed"
//                 };
//             }
//         }
//         catch (Exception ex)
//         {
//             _logger.LogError(ex, "Error validating JWT token");
//             return new TokenValidationResult
//             {
//                 IsValid = false,
//                 Error = ex.Message
//             };
//         }
//     }
//
//     /// <summary>
//     /// Obtém as chaves de assinatura do Keycloak JWKS endpoint
//     /// </summary>
//     private IEnumerable<SecurityKey> GetKeycloakSigningKeys()
//     {
//         // Esta implementação seria expandida para buscar chaves do endpoint JWKS do Keycloak
//         // Por enquanto, retorna uma lista vazia - o ASP.NET Core fará isso automaticamente
//         return Enumerable.Empty<SecurityKey>();
//     }
//
//     /// <summary>
//     /// Cria um ClaimsPrincipal com as claims extraídas do token
//     /// </summary>
//     private ClaimsPrincipal CreateClaimsPrincipal(IEnumerable<Claim> claims)
//     {
//         var claimsIdentity = new ClaimsIdentity(claims, Scheme.Name);
//
//         // Adiciona claims personalizadas se necessário
//         EnrichUserClaims(claimsIdentity);
//
//         return new ClaimsPrincipal(claimsIdentity);
//     }
//
//     /// <summary>
//     /// Enriquece as claims do usuário com informações adicionais
//     /// </summary>
//     private void EnrichUserClaims(ClaimsIdentity identity)
//     {
//         // Extrai roles do token Keycloak (formato específico)
//         var realmAccess = identity.FindFirst("realm_access")?.Value;
//         if (!string.IsNullOrEmpty(realmAccess))
//         {
//             // Parse do JSON realm_access para extrair roles
//             // Implementação específica para o formato do Keycloak
//         }
//
//         // Adiciona timestamp da autenticação
//         identity.AddClaim(new Claim("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()));
//
//         // Adiciona informações do request
//         identity.AddClaim(new Claim("request_ip", Context.Connection.RemoteIpAddress?.ToString() ?? "unknown"));
//     }
//
//     /// <summary>
//     /// Resultado da validação do token
//     /// </summary>
//     private class TokenValidationResult
//     {
//         public bool IsValid { get; set; }
//         public string? Error { get; set; }
//         public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();
//         public JwtSecurityToken? Token { get; set; } // ADICIONADO - propriedade faltante
//     }
//     private bool IsTokenExpired(JwtSecurityToken token)
//     {
//         var now = _timeProvider.GetUtcNow(); // Usar TimeProvider
//         return token.ValidTo <= now;
//     }
// }