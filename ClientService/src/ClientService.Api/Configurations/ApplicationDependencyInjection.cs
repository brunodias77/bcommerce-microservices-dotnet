using ClientService.Application.Services;
using ClientService.Application.UseCases.Clients.Create;
using ClientService.Domain.Common;
using ClientService.Domain.Events.Clients;
using ClientService.Infra.Services;

namespace ClientService.Api.Configurations;

public static class ApplicationDependencyInjection
{
    public static void AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        AddUseCases(services);
        AddServices(services);
        AddEvents(services, configuration);
    }

    private static void AddUseCases(IServiceCollection services)
    {
        services.AddScoped<ICreateClientUseCase, CreateClientUseCase>();
        // services.AddScoped<ILoginClientUseCase, LoginClientUseCase>();
    }

    private static void AddServices(IServiceCollection services)
    {
        services.AddScoped<IKeycloakService, KeycloakService>();
    }

    private static void AddEvents(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IDomainEventHandler<ClientCreatedEvent>, ClientCreatedEventHandler>();
    }
}