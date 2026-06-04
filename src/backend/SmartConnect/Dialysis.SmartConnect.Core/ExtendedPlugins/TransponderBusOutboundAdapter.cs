using System.Collections.Immutable;
using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.SmartConnect.Contracts.Integration;
using Microsoft.Extensions.DependencyInjection;

namespace Dialysis.SmartConnect.ExtendedPlugins;

/// <summary>
/// Outbound adapter (kind <c>transponder-bus</c>) that publishes the transformed route payload onto
/// the Transponder bus as a <see cref="SmartConnectRoutedPayloadIntegrationEvent"/>. Consumers
/// across other modules subscribe via <c>IConsumer&lt;SmartConnectRoutedPayloadIntegrationEvent&gt;</c>
/// and fan out by <c>RoutingHint</c>.
/// </summary>
/// <remarks>
/// Use this when a flow needs to broadcast its output to the cross-module bus without committing to
/// a module-owned typed event. For module-specific contracts prefer a dedicated typed publisher.
/// Schema-versioned via <see cref="SmartConnectRoutedPayloadIntegrationEvent.SchemaVersion"/>;
/// bumps go through <c>IntegrationEventVersioningTests</c>.
///
/// Resolves <see cref="ITransponderBus"/> from a fresh DI scope on each send so SmartConnect.Core
/// hosts that do not wire a bus (some unit-test compositions) can still register the adapter without
/// failing at startup — a missing bus surfaces as an actionable per-send failure instead.
/// </remarks>
public sealed class TransponderBusOutboundAdapter : IOutboundAdapter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _time;
    /// <summary>
    /// Outbound adapter (kind <c>transponder-bus</c>) that publishes the transformed route payload onto
    /// the Transponder bus as a <see cref="SmartConnectRoutedPayloadIntegrationEvent"/>. Consumers
    /// across other modules subscribe via <c>IConsumer&lt;SmartConnectRoutedPayloadIntegrationEvent&gt;</c>
    /// and fan out by <c>RoutingHint</c>.
    /// </summary>
    /// <remarks>
    /// Use this when a flow needs to broadcast its output to the cross-module bus without committing to
    /// a module-owned typed event. For module-specific contracts prefer a dedicated typed publisher.
    /// Schema-versioned via <see cref="SmartConnectRoutedPayloadIntegrationEvent.SchemaVersion"/>;
    /// bumps go through <c>IntegrationEventVersioningTests</c>.
    ///
    /// Resolves <see cref="ITransponderBus"/> from a fresh DI scope on each send so SmartConnect.Core
    /// hosts that do not wire a bus (some unit-test compositions) can still register the adapter without
    /// failing at startup — a missing bus surfaces as an actionable per-send failure instead.
    /// </remarks>
    public TransponderBusOutboundAdapter(IServiceScopeFactory scopeFactory, TimeProvider time)
    {
        _scopeFactory = scopeFactory;
        _time = time;
    }
    public const string KindValue = "transponder-bus";

    private const string ParametersMetadataKey = "smartconnect.outbound.parameters";

    public string Kind => KindValue;

    public async Task<OutboundSendResult> SendAsync(
        IntegrationMessage message,
        int outboundRouteOrdinal,
        CancellationToken cancellationToken)
    {
        TransponderBusOutboundParameters parameters;
        if (message.Metadata.TryGetValue(ParametersMetadataKey, out var json) && !string.IsNullOrWhiteSpace(json))
        {
            try
            {
                parameters = JsonSerializer.Deserialize<TransponderBusOutboundParameters>(json)
                    ?? new TransponderBusOutboundParameters();
            }
            catch (JsonException ex)
            {
                return new OutboundSendResult(false, $"transponder-bus parameters JSON is invalid: {ex.Message}");
            }
        }
        else
        {
            parameters = new TransponderBusOutboundParameters();
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var bus = scope.ServiceProvider.GetService<ITransponderBus>();
        if (bus is null)
        {
            return new OutboundSendResult(
                false,
                "transponder-bus adapter requires ITransponderBus in the DI container; register Transponder on the host before using this destination.");
        }

        var headers = BuildHeaderDictionary(message, parameters);
        var envelope = new SmartConnectRoutedPayloadIntegrationEvent(
            EventId: Guid.CreateVersion7(),
            OccurredOn: _time.GetUtcNow().UtcDateTime,
            SchemaVersion: 1,
            FlowId: message.FlowId,
            IntegrationMessageId: message.Id,
            OutboundRouteOrdinal: outboundRouteOrdinal,
            RoutingHint: parameters.RoutingHint ?? string.Empty,
            PayloadFormat: message.PayloadFormat.ToString(),
            Payload: message.Payload.ToArray(),
            Headers: headers);

        var publishOptions = new TransponderPublishOptions(
            CorrelationId: message.CorrelationId,
            DeduplicationId: parameters.DeduplicationId ?? message.Id.ToString("N"));

        try
        {
            await bus.PublishAsync(envelope, publishOptions, cancellationToken).ConfigureAwait(false);
            return new OutboundSendResult(true, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new OutboundSendResult(false, $"transponder-bus publish failed: {ex.Message}");
        }
    }

    public string? GetParametersSchema() => """
        {
          "$schema": "https://json-schema.org/draft/2020-12/schema",
          "title": "TransponderBusOutboundParameters",
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "routingHint": {
              "type": "string",
              "description": "Operator-supplied hint surfaced on the envelope (e.g. an HL7 trigger event like 'ORU^R01' or a FHIR ResourceType). Consumers route on this without parsing the payload."
            },
            "deduplicationId": {
              "type": ["string", "null"],
              "description": "Optional broker dedup key. Defaults to the integration message id."
            },
            "headers": {
              "type": "object",
              "additionalProperties": { "type": "string" },
              "description": "Extra string headers copied onto the envelope (sender, payload format, etc.)."
            }
          }
        }
        """;

    private static IReadOnlyDictionary<string, string> BuildHeaderDictionary(
        IntegrationMessage message,
        TransponderBusOutboundParameters parameters)
    {
        var builder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.Ordinal);
        if (parameters.Headers is not null)
        {
            foreach (var (k, v) in parameters.Headers)
            {
                if (!string.IsNullOrWhiteSpace(k) && v is not null)
                {
                    builder[k] = v;
                }
            }
        }

        if (!builder.ContainsKey("smartconnect.correlationId") && !string.IsNullOrWhiteSpace(message.CorrelationId))
        {
            builder["smartconnect.correlationId"] = message.CorrelationId;
        }

        return builder.ToImmutable();
    }
}

/// <summary>
/// Parameter shape for <see cref="TransponderBusOutboundAdapter"/>. Stored in
/// <c>OutboundRouteSlot.OutboundParametersJson</c>.
/// </summary>
public sealed class TransponderBusOutboundParameters
{
    public string? RoutingHint { get; set; }

    public string? DeduplicationId { get; set; }

    public Dictionary<string, string>? Headers { get; set; }
}
