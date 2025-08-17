using ClientService.Domain.Common;
using ClientService.Domain.Events.Clients;
using Microsoft.Extensions.Logging;

namespace ClientService.Infra.Services;

public class ClientCreatedEventHandler : IDomainEventHandler<ClientCreatedEvent>
{
    private readonly ILogger<ClientCreatedEventHandler> _logger;

    public ClientCreatedEventHandler(ILogger<ClientCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(ClientCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Client created: {ClientId} - {Email}", 
            domainEvent.ClientId, 
            domainEvent.Email);

        // Aqui você pode adicionar lógicas como:
        // - Enviar email de boas-vindas
        // - Criar perfil no Keycloak
        // - Notificar outros serviços
        // - Etc.

        await Task.CompletedTask;
    }
}