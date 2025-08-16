using System.Security.Claims;
using System.Text.Json;
using ClientService.Api.Configurations;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

// Carregar variáveis do arquivo .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddHttpContextAccessor();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Client Service API", 
        Version = "v1",
        Description = "API para gerenciamento de clientes"
    });
    
    // Configuração para JWT Bearer no Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient("keycloak", client =>
{
    var keycloakConfig = builder.Configuration.GetSection("Keycloak");
    client.BaseAddress = new Uri(keycloakConfig["AdminBaseUrl"] ?? "http://localhost:8080/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var keycloakConfig = builder.Configuration.GetSection("Keycloak");
        
        options.Authority = keycloakConfig["Authority"];
        options.RequireHttpsMetadata = false; // Apenas para desenvolvimento
        options.Audience = keycloakConfig["Audience"];
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = keycloakConfig["Authority"],
            ValidAudience = keycloakConfig["Audience"],
            ClockSkew = TimeSpan.FromMinutes(5),
            // Configuração correta para roles do Keycloak
            RoleClaimType = "realm_access/roles",
            NameClaimType = "preferred_username"
        };
        
        // Eventos JWT para tratamento de erros
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Add("Token-Expired", "true");
                }
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                
                var result = JsonSerializer.Serialize(new
                {
                    error = "unauthorized",
                    message = "Token de acesso requerido",
                    timestamp = DateTime.UtcNow
                });
                
                return context.Response.WriteAsync(result);
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                
                var result = JsonSerializer.Serialize(new
                {
                    error = "forbidden",
                    message = "Acesso negado. Permissões insuficientes",
                    timestamp = DateTime.UtcNow
                });
                
                return context.Response.WriteAsync(result);
            },
            OnTokenValidated = context =>
            {
                // Processar claims do Keycloak
                var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                if (claimsIdentity != null)
                {
                    // Extrair roles do token Keycloak
                    var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrEmpty(realmAccess))
                    {
                        var realmAccessObj = JsonSerializer.Deserialize<JsonElement>(realmAccess);
                        if (realmAccessObj.TryGetProperty("roles", out var rolesElement))
                        {
                            foreach (var role in rolesElement.EnumerateArray())
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, role.GetString() ?? ""));
                            }
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// Configuração de Autorização
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("USER", "ADMIN"));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Client Service API V1");
        c.RoutePrefix = "swagger"; // Manter o prefixo swagger
    });
}
app.UseHttpsRedirection();

// ORDEM CORRETA: Authentication ANTES de Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();