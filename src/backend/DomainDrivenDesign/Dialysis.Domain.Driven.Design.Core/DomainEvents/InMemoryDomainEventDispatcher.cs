using Dialysis.DomainDrivenDesign.DomainEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.DomainDrivenDesign.DomainEvents;

/// <summary>
/// Default <see cref="IDomainEventDispatcher"/> that resolves every registered
/// <see cref="IDomainEventHandler{TEvent}"/> for the runtime type of the event and invokes them
/// in registration order. Failures are collected and re-thrown as <see cref="AggregateException"/>
/// so one failing handler does not silently skip its siblings.
/// </summary>
public sealed class InMemoryDomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = (IEnumerable<object>)serviceProvider.GetServices(handlerType);

        List<Exception>? failures = null;
        foreach (var handler in handlers)
        {
            try
            {
                var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
                var task = (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (failures ??= []).Add(ex);
            }
        }

        if (failures is { Count: > 0 })
        {
            throw new AggregateException("One or more domain-event handlers failed.", failures);
        }
    }
}
