// Importações necessárias para autenticação JWT, políticas de resiliência, logging, etc.
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using ApiGateway.Extensions;
using ApiGateway.Infrastructure.Authentication; // Nova importação
using System.Text;

// CONFIGURAÇÃO DO SERILOG (Sistema de Logging Avançado)
// Serilog é uma biblioteca de logging estruturado que permite rastrear o que acontece na aplicação
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information() // Nível mínimo de log (Debug < Information < Warning < Error < Fatal)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning) // Reduz logs verbosos do ASP.NET Core
    .MinimumLevel.Override("Yarp", LogEventLevel.Information) // Mantém logs do YARP visíveis
    .Enrich.FromLogContext() // Adiciona informações do contexto atual
    .Enrich.WithEnvironmentName() // Adiciona o nome do ambiente (Development, Production, etc.)
    .Enrich.WithMachineName() // Adiciona o nome da máquina
    .Enrich.WithProcessId() // Adiciona o ID do processo
    .Enrich.WithThreadId() // Adiciona o ID da thread
    // Configuração para exibir logs no console com formato personalizado
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    // Configuração para salvar logs em arquivos diários
    .WriteTo.File("logs/gateway-.txt", 
        rollingInterval: RollingInterval.Day, // Cria um novo arquivo por dia
        retainedFileCountLimit: 7, // Mantém apenas 7 dias de logs
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting API Gateway with Advanced YARP Configuration");
    
    // CRIAÇÃO DO BUILDER DA APLICAÇÃO
    // O WebApplicationBuilder é usado para configurar todos os serviços da aplicação
    var builder = WebApplication.CreateBuilder(args);
    
    // Configura o Serilog como o sistema de logging padrão
    builder.Host.UseSerilog();
    
    // CONFIGURAÇÃO DO REDIS (Cache Distribuído e Pub/Sub)
    // Redis é usado para cache distribuído e comunicação entre instâncias do gateway
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    {
        return ConnectionMultiplexer.Connect(redisConnectionString);
    });
    
    // Configura o Redis como cache distribuído
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "ApiGateway"; // Nome da instância para identificação
    });
    
    // CONFIGURAÇÃO DO OPENTELEMETRY (Observabilidade)
    // OpenTelemetry fornece tracing distribuído e métricas para monitoramento
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ApiGateway", "1.0.0")) // Identifica o serviço
            .AddAspNetCoreInstrumentation() // Rastreia requisições HTTP
            .AddHttpClientInstrumentation()) // Rastreia chamadas HTTP para outros serviços
        .WithMetrics(metrics => metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ApiGateway", "1.0.0"))
            .AddAspNetCoreInstrumentation() // Coleta métricas do ASP.NET Core
            .AddHttpClientInstrumentation() // Coleta métricas de HTTP clients
            .AddPrometheusExporter()); // Exporta métricas no formato Prometheus
    
    // CONFIGURAÇÃO DE HEALTH CHECKS (Verificação de Saúde)
    // Health checks verificam se os serviços dependentes estão funcionando
    builder.Services.AddHealthChecks()
        .AddRedis(redisConnectionString, name: "redis") // Verifica se o Redis está funcionando
        .AddUrlGroup(new Uri("http://localhost:5122/health"), name: "client-service") // Verifica o serviço de clientes
        .AddUrlGroup(new Uri("http://localhost:5123/health"), name: "catalog-service") // Verifica o serviço de catálogo
        .AddUrlGroup(new Uri("http://localhost:5124/health"), name: "cart-service") // Verifica o serviço de carrinho
        .AddUrlGroup(new Uri("http://localhost:5125/health"), name: "sales-service"); // Verifica o serviço de vendas
    
    // CONFIGURAÇÃO DO YARP AVANÇADO (Reverse Proxy)
    // Adiciona o YARP com configurações personalizadas
    builder.Services.AddAdvancedYarp(builder.Configuration);
    
    // CONFIGURAÇÃO DE AUTENTICAÇÃO JWT PERSONALIZADA (JSON Web Tokens)
    // Configuração das opções do Keycloak
    var keycloakConfig = builder.Configuration.GetSection("Keycloak");
    builder.Services.Configure<KeycloakAuthenticationOptions>(options =>
    {
        options.Authority = keycloakConfig["Authority"] ?? "";
        options.Audience = keycloakConfig["Audience"] ?? "b-commerce-backend";
        options.RequireHttpsMetadata = keycloakConfig.GetValue<bool>("RequireHttpsMetadata");
        var validIssuersList = keycloakConfig.GetSection("ValidIssuers").Get<List<string>>() ?? new List<string> { keycloakConfig["Authority"] ?? "" };
        options.ValidIssuers = validIssuersList.ToArray(); // Conversão para string[]
        options.PublicEndpoints = new List<string>
        {
            "/health",
            "/health/ready",
            "/health/live",
            "/metrics",
            "/api/auth/login",
            "/api/auth/register"
        };
    });

    // Registra os serviços de autenticação personalizados
    builder.Services.AddSingleton(TimeProvider.System); // Adicionar TimeProvider
    builder.Services.AddScoped<JwtAuthenticationHandler>();
    builder.Services.AddScoped<IUserContextService, UserContextService>();

    // Configuração JWT com nosso handler personalizado
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var keycloakOptions = new KeycloakAuthenticationOptions();
            keycloakConfig.Bind(keycloakOptions);
            
            options.Authority = keycloakOptions.Authority;
            options.RequireHttpsMetadata = keycloakOptions.RequireHttpsMetadata;
            
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuers = keycloakOptions.ValidIssuers,
                ValidAudiences = new[] { "account", keycloakOptions.Audience },
                ClockSkew = TimeSpan.FromMinutes(5)
            };
            
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Log.Warning("JWT Authentication failed: {Exception} for path {Path}", 
                        context.Exception.Message, context.Request.Path);
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var userContextService = context.HttpContext.RequestServices.GetRequiredService<IUserContextService>();
                    var userContext = userContextService.ExtractUserContext(context.Principal);
                    
                    Log.Debug("Token validated for user: {UserId} ({Username}) with roles: {Roles}", 
                        userContext.UserId, userContext.Username, string.Join(", ", userContext.Roles));
                        
                    // Adiciona o contexto do usuário ao HttpContext para uso posterior
                    context.HttpContext.Items["UserContext"] = userContext;
                },
                OnChallenge = context =>
                {
                    Log.Warning("JWT Authentication challenge for path {Path}: {Error}", 
                        context.Request.Path, context.Error);
                    return Task.CompletedTask;
                }
            };
        });
    
    // CONFIGURAÇÃO DE AUTORIZAÇÃO (Políticas de Acesso)
    // Define quem pode acessar quais recursos
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser()); // Usuário logado
        options.AddPolicy("admin", policy => policy.RequireRole("admin")); // Apenas administradores
        options.AddPolicy("customer", policy => policy.RequireRole("customer")); // Apenas clientes
    });
    
    // CONFIGURAÇÃO DE HTTP CLIENTS COM POLLY (Resiliência)
    // Polly adiciona políticas de retry e circuit breaker para tornar as chamadas HTTP mais resilientes
    builder.Services.AddHttpClient("client-service")
        .AddPolicyHandler(GetRetryPolicy()) // Política de retry (tentar novamente em caso de falha)
        .AddPolicyHandler(GetCircuitBreakerPolicy()); // Circuit breaker (para de tentar se muitas falhas)
    
    builder.Services.AddHttpClient("catalog-service")
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    builder.Services.AddHttpClient("cart-service")
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    builder.Services.AddHttpClient("sales-service")
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy());
    
    // CONFIGURAÇÃO DE CORS (Cross-Origin Resource Sharing)
    // CORS permite que o frontend (ex: Angular) acesse o backend
    var corsConfig = builder.Configuration.GetSection("CORS");
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            // Origens permitidas (ex: http://localhost:4200 para Angular)
            var allowedOrigins = corsConfig.GetSection("AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:4200" };
            // Métodos HTTP permitidos
            var allowedMethods = corsConfig.GetSection("AllowedMethods").Get<string[]>() ?? new[] { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
            // Cabeçalhos permitidos
            var allowedHeaders = corsConfig.GetSection("AllowedHeaders").Get<string[]>() ?? new[] { "*" };
            // Se permite credenciais (cookies, headers de autorização)
            var allowCredentials = corsConfig.GetValue<bool>("AllowCredentials");
            
            policy.WithOrigins(allowedOrigins)
                  .WithMethods(allowedMethods)
                  .WithHeaders(allowedHeaders);
            
            if (allowCredentials)
                policy.AllowCredentials();
        });
    });
    
    // CONSTRUÇÃO DA APLICAÇÃO
    var app = builder.Build();
    
    // CONFIGURAÇÃO DO PIPELINE DE MIDDLEWARE
    // Middleware são componentes que processam requisições HTTP em ordem
    
    // Em desenvolvimento, mostra páginas de erro detalhadas
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    
    // MIDDLEWARE DE LOGGING DE REQUISIÇÕES
    // Registra todas as requisições HTTP com detalhes
    app.UseSerilogRequestLogging(options =>
    {
        // Template da mensagem de log
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        
        // Adiciona informações extras ao contexto de log
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
            diagnosticContext.Set("RequestId", httpContext.Request.Headers["X-Request-ID"].FirstOrDefault());
            
            // Usa nosso serviço de contexto de usuário para informações mais detalhadas
            if (httpContext.Items.TryGetValue("UserContext", out var userContextObj) && 
                userContextObj is UserContext userContext)
            {
                diagnosticContext.Set("UserId", userContext.UserId);
                diagnosticContext.Set("Username", userContext.Username);
                diagnosticContext.Set("UserEmail", userContext.Email);
                diagnosticContext.Set("UserRoles", string.Join(",", userContext.Roles));
            }
        };
    });
    
    // Ordem dos middlewares é importante!
    app.UseCors("AllowFrontend"); // CORS deve vir antes de autenticação
    
    // Adiciona nosso middleware personalizado de autenticação JWT
    app.UseMiddleware<JwtAuthenticationMiddleware>();
    
    app.UseAuthentication(); // Autenticação deve vir antes de autorização
    app.UseAuthorization(); // Autorização
    
    // ENDPOINTS DE HEALTH CHECK
    // Endpoints para verificar se o gateway está funcionando
    app.MapHealthChecks("/health"); // Health check geral
    app.MapHealthChecks("/health/ready"); // Verifica se está pronto para receber tráfego
    app.MapHealthChecks("/health/live"); // Verifica se está vivo
    
    // ENDPOINT DE MÉTRICAS PROMETHEUS
    // Expõe métricas para monitoramento
    app.MapPrometheusScrapingEndpoint();
    
    // ENDPOINTS DE GERENCIAMENTO DE CONFIGURAÇÃO
    // Endpoints para administradores gerenciarem a configuração do gateway
    app.MapGet("/api/gateway/config", async (ApiGateway.Services.IConfigurationService configService) =>
    {
        var config = await configService.GetCurrentConfigurationAsync();
        return Results.Ok(config);
    }).RequireAuthorization("admin"); // Apenas administradores podem ver a configuração
    
    app.MapPost("/api/gateway/config/reload", async (ApiGateway.Services.IConfigurationService configService) =>
    {
        var result = await configService.ReloadConfigurationAsync();
        return result ? Results.Ok("Configuration reloaded") : Results.BadRequest("Failed to reload configuration");
    }).RequireAuthorization("admin"); // Apenas administradores podem recarregar a configuração
    
    // MAPEAMENTO DO YARP (Reverse Proxy)
    // Esta linha ativa o reverse proxy - todas as requisições não mapeadas acima serão processadas pelo YARP
    app.MapReverseProxy();
    
    Log.Information("API Gateway started successfully with Advanced YARP Configuration");
    app.Run(); // Inicia a aplicação
}
catch (Exception ex)
{
    // Se algo der errado durante a inicialização, registra o erro
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    // Sempre fecha o sistema de logging ao finalizar
    Log.CloseAndFlush();
}

// POLÍTICAS POLLY PARA RESILIÊNCIA

/// <summary>
/// Política de Retry - Tenta novamente em caso de falha
/// Implementa backoff exponencial: 2s, 4s, 8s
/// </summary>
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // Trata erros temporários (timeouts, 5xx, etc.)
        .WaitAndRetryAsync(
            retryCount: 3, // Máximo 3 tentativas
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2^1, 2^2, 2^3 segundos
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log quando faz retry
                Log.Warning("Retry {RetryCount} for {ServiceName} after {Delay}ms. Reason: {Reason}", 
                    retryCount, context.OperationKey, timespan.TotalMilliseconds, outcome.Exception?.Message ?? "HTTP Error");
            });
}

/// <summary>
/// Política de Circuit Breaker - Para de tentar se muitas falhas consecutivas
/// Protege o sistema de sobrecarga quando um serviço está falhando
/// </summary>
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // Trata erros temporários
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 3, // Após 3 falhas consecutivas, abre o circuito
            durationOfBreak: TimeSpan.FromSeconds(30), // Fica aberto por 30 segundos
            onBreak: (exception, duration) =>
            {
                // Log quando o circuito abre
                Log.Error("Circuit breaker opened for {Duration}s. Reason: {Reason}", 
                    duration.TotalSeconds, exception.Exception?.Message ?? "Unknown error");
            },
            onReset: () =>
            {
                // Log quando o circuito fecha (serviço voltou ao normal)
                Log.Information("Circuit breaker reset - service is healthy again");
            },
            onHalfOpen: () =>
            {
                // Log quando o circuito está meio-aberto (testando se o serviço voltou)
                Log.Information("Circuit breaker half-open - testing service health");
            });
}