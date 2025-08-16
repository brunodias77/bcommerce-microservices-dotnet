using ClientService.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ClientService.Infra.Services;

public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(IServiceProvider serviceProvider, ILogger<DomainEventPublisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : DomainEvent
    {
        _logger.LogInformation("Publishing domain event {EventType}", typeof(T).Name);

        try
        {
            // Resolver o handler para o tipo de evento específico
            var handler = _serviceProvider.GetService<IDomainEventHandler<T>>();
            
            if (handler != null)
            {
                _logger.LogInformation("Found handler {HandlerType} for event {EventType}", 
                    handler.GetType().Name, typeof(T).Name);
                    
                await handler.Handle(domainEvent, cancellationToken);
                
                _logger.LogInformation("Successfully processed event {EventType}", typeof(T).Name);
            }
            else
            {
                _logger.LogWarning("No handler found for event {EventType}", typeof(T).Name);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing domain event {EventType}", typeof(T).Name);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var method = typeof(DomainEventPublisher).GetMethod(nameof(PublishAsync), new[] { eventType, typeof(CancellationToken) });
            
            if (method != null)
            {
                var task = (Task)method.Invoke(this, new object[] { domainEvent, cancellationToken })!;
                await task;
            }
        }
    }
}