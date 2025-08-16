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
        _logger.LogDebug("Publishing domain event {EventType}", typeof(T).Name);

        var handler = _serviceProvider.GetService<IDomainEventHandler<T>>();
        
        if (handler == null)
        {
            _logger.LogWarning("No handler found for event {EventType}", typeof(T).Name);
            return;
        }

        try
        {
            await handler.Handle(domainEvent, cancellationToken);
            _logger.LogDebug("Event {EventType} processed successfully", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing domain event {EventType}", typeof(T).Name);
            throw;
        }
    }

    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        var tasks = domainEvents.Select(domainEvent => PublishEventAsync(domainEvent, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task PublishEventAsync(DomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var eventType = domainEvent.GetType();
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handler = _serviceProvider.GetService(handlerType);

        if (handler == null)
        {
            _logger.LogWarning("No handler found for event {EventType}", eventType.Name);
            return;
        }

        try
        {
            var handleMethod = handlerType.GetMethod("Handle");
            var task = (Task)handleMethod!.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
            await task;
            _logger.LogDebug("Event {EventType} processed successfully", eventType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing domain event {EventType}", eventType.Name);
            throw;
        }
    }
}