using System.Text.Json;

using Dialysis.SharedKernel.Abstractions;

using Microsoft.Extensions.Logging;

using Transponder.Abstractions;

namespace Dialysis.Gateway.Infrastructure;

/// <summary>
/// Publishes domain events to Azure Service Bus via Transponder. Phase 2.3.2.
/// </summary>
public sealed class AzureServiceBusEventExportPublisher : IEventExportPublisher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<AzureServiceBusEventExportPublisher> _logger;

    public AzureServiceBusEventExportPublisher(
        IPublishEndpoint publishEndpoint,
        ILogger<AzureServiceBusEventExportPublisher> logger)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync(string eventType, object payload, CancellationToken cancellationToken = default)
    {
        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);
        var message = new EventExportMessage(eventType, payloadJson);
        await _publishEndpoint.PublishAsync(message, cancellationToken);
        _logger.LogDebug("Published {EventType} to Azure Service Bus", eventType);
    }
}
