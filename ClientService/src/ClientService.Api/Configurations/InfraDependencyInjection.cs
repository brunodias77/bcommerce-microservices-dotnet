using System.Security.Claims;
using System.Text.Json;
using ClientService.Domain.Common;
using ClientService.Domain.Repositories;
using ClientService.Domain.Services;
using ClientService.Infra.Data;
using ClientService.Infra.Repositories;
using ClientService.Infra.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ClientService.Api.Configurations;

public static class InfraDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddDatabase(services, configuration);
        AddRepositories(services);
        AddLoggedUser(services);
        AddEvents(services);
        AddAuthentication(services, configuration);
        AddHttpClients(services, configuration);
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    private static void AddEvents(IServiceCollection services)
    {
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
    }

    private static void AddDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<ClientDbContext>(options =>
            options.UseNpgsql(connectionString));
    }

    private static void AddLoggedUser(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenProvider, TokenProvider>();
        services.AddScoped<ILoggedUser, LoggedUser>();
    }

    private static void AddAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var keycloakConfig = configuration.GetSection("Keycloak");

                options.Authority = keycloakConfig["Authority"];
                options.RequireHttpsMetadata = false; // Apenas para desenvolvimento

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = keycloakConfig["Authority"],
                    ValidAudiences = new[] { "account", "b-commerce-backend" },
                    ClockSkew = TimeSpan.FromMinutes(5),
                    RoleClaimType = "realm_access/roles",
                    NameClaimType = "preferred_username"
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("Token validated successfully");
                        var claims = context.Principal?.Claims?.Select(c => $"{c.Type}: {c.Value}") ?? Enumerable.Empty<string>();
                        Console.WriteLine($"Claims: {string.Join(", ", claims)}");
                        
                        var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
                        if (claimsIdentity != null)
                        {
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
                    }
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("ADMIN"));
            options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("USER", "ADMIN"));
        });
    }

    private static void AddHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("keycloak", client =>
        {
            var keycloakConfig = configuration.GetSection("Keycloak");
            client.BaseAddress = new Uri(keycloakConfig["AdminBaseUrl"] ?? "http://localhost:8080/");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    }
}