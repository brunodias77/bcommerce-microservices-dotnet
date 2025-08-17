using ClientService.Application.Services;
using ClientService.Application.UseCases.Clients.Create;
using ClientService.Application.UseCases.Clients.GetProfile;
using ClientService.Application.UseCases.Clients.Login;
using ClientService.Domain.Common;
using ClientService.Domain.Events.Clients;
using ClientService.Infra.Services;
using Microsoft.OpenApi.Models;

namespace ClientService.Api.Configurations;

public static class ApplicationDependencyInjection
{
    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        AddUseCases(services);
        AddServices(services);
        AddEvents(services, configuration);
        AddSwagger(services);
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<ICreateClientUseCase, CreateClientUseCase>();
        services.AddScoped<ILoginClientUseCase, LoginClientUseCase>();
        services.AddScoped<IGetClientProfileUseCase, GetClientProfileUseCase>();
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IKeycloakService, KeycloakService>();
    }

    private static void AddEvents(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventHandler<ClientCreatedEvent>, ClientCreatedEventHandler>();
    }

    private static void AddSwagger(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Client Service API",
                Version = "v1",
                Description = "API para gerenciamento de clientes"
            });

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
    }
}