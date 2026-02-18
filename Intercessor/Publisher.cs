
using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Intercessor;

internal class Publisher : IPublisher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Publisher> _logger;

    public Publisher(IServiceProvider serviceProvider, ILogger<Publisher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        Type notificationType = typeof(TNotification);
        Type handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

        var handlers = (_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(handlerType)) as IEnumerable<object>
                        ?? [])
            .Cast<dynamic>()
            .ToList();

        var tasks = handlers.Select(async handler =>
        {
            try
            {
                _logger.LogTrace("[Intercessor] Publisher: Publishing notification {NotificationName}.", notificationType.FullName);
                await handler.HandleAsync((dynamic)notification, cancellationToken);
            }
            catch (Exception ex)
            {
                string handlerName = handler.GetType().FullName ?? handler.GetType().Name;
                string notificationName = notificationType.FullName ?? notificationType.Name;
                throw new InvalidOperationException(
                    $"Error handling notification {notificationName} with handler {handlerName}.",
                    ex);
            }
        }).ToList();

        await Task.WhenAll(tasks);
    }
}
