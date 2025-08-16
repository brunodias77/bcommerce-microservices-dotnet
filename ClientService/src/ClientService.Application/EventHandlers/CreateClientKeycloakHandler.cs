using ClientService.Application.Common;
using ClientService.Domain.Aggregates;
using ClientService.Domain.Common;
using ClientService.Domain.Events.Clients;
using ClientService.Domain.Repositories;
using ClientService.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ClientService.Application.EventHandlers;

public class CreateClientKeycloakHandler : IDomainEventHandler<CreateClientKeycloak>
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventPublisher _domainEventPublisher;
    private readonly ILogger<CreateClientKeycloakHandler> _logger;

    public CreateClientKeycloakHandler(
        IClientRepository clientRepository,
        IUnitOfWork unitOfWork,
        IDomainEventPublisher domainEventPublisher,
        ILogger<CreateClientKeycloakHandler> logger)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
        _domainEventPublisher = domainEventPublisher;
        _logger = logger;
    }

    public async Task Handle(CreateClientKeycloak domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing CreateClientKeycloak event for user {KeycloakUserId}", 
            domainEvent.KeycloakUserId);

        try
        {
            // Verificar se o cliente já existe
            var existingClient = await _clientRepository.GetByEmailAsync(domainEvent.Email, cancellationToken);
            if (existingClient != null)
            {
                _logger.LogWarning("Client with email {Email} already exists", domainEvent.Email);
                return;
            }

            // Criar a entidade Client
            var client = Client.Create(
                Guid.Parse(domainEvent.KeycloakUserId),
                domainEvent.FirstName,
                domainEvent.LastName,
                domainEvent.Email,
                string.Empty, // PasswordHash vazio pois a senha está no Keycloak
                domainEvent.Role.ToString()
            );

            // Adicionar propriedades opcionais se fornecidas
            if (!string.IsNullOrEmpty(domainEvent.Phone))
            {
                // client.SetPhone(Phone.Create(domainEvent.Phone));
            }

            if (domainEvent.DateOfBirth.HasValue)
            {
                // client.SetDateOfBirth(domainEvent.DateOfBirth.Value);
            }

            if (!string.IsNullOrEmpty(domainEvent.Cpf))
            {
                // client.SetCpf(Cpf.Create(domainEvent.Cpf));
            }

            // Salvar no banco de dados
            await _clientRepository.AddAsync(client, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Disparar evento ClientCreated após sucesso
            var clientCreatedEvent = new ClientCreated(
                client.Id,
                client.Email.Value,
                client.FirstName,
                client.LastName,
                client.Role,
                domainEvent.KeycloakUserId,
                domainEvent.Phone,
                domainEvent.DateOfBirth,
                domainEvent.Cpf
            );

            await _domainEventPublisher.PublishAsync(clientCreatedEvent, cancellationToken);

            _logger.LogInformation("Client created successfully for Keycloak user {KeycloakUserId} with ID {ClientId}", 
                domainEvent.KeycloakUserId, client.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CreateClientKeycloak event for user {KeycloakUserId}", 
                domainEvent.KeycloakUserId);
            throw;
        }
    }
}