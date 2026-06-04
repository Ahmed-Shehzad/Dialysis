using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.HIE.Contracts.Integration;
using Dialysis.HIE.OpenEhr;
using Dialysis.HIE.Outbound.Domain;
using Dialysis.HIE.Outbound.Partners;
using Dialysis.HIE.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Outbound.Dispatch;

public sealed class OutboundDispatcher : IOutboundDispatcher
{
    private static readonly FhirJsonDeserializer _parser = new(new DeserializerSettings().UsingMode(DeserializationMode.Recoverable));
    private readonly OutboundOptions _options;
    private readonly IOutboundBundleStore _store;
    private readonly IPartnerEndpointResolver _resolver;
    private readonly CompositionWriter? _compositionWriter;
    private readonly ITransponderOutbox? _transponderOutbox;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<OutboundDispatcher> _logger;
    public OutboundDispatcher(IOutboundBundleStore store,
        IPartnerEndpointResolver resolver,
        CompositionWriter? compositionWriter,
        ITransponderOutbox? transponderOutbox,
        TimeProvider timeProvider,
        IOptions<OutboundOptions> options,
        ILogger<OutboundDispatcher> logger)
    {
        _store = store;
        _resolver = resolver;
        _compositionWriter = compositionWriter;
        _transponderOutbox = transponderOutbox;
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<int> TickAsync(CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var batch = await _store.ClaimPendingAsync(_options.DispatchBatchSize, now, cancellationToken).ConfigureAwait(false);
        if (batch.Count == 0)
            return 0;

        var processed = 0;
        foreach (var bundle in batch)
        {
            await DeliverOneAsync(bundle, cancellationToken).ConfigureAwait(false);
            processed += 1;
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return processed;
    }

    private async Task DeliverOneAsync(OutboundBundle bundle, CancellationToken cancellationToken)
    {
        var endpoint = _resolver.Resolve(bundle.PartnerId);
        if (endpoint is null)
        {
            var retryAt = _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.BackoffSeconds);
            bundle.MarkAttemptFailed($"No partner endpoint registered for {bundle.PartnerId}", retryAt, _options.MaxAttempts);
            _logger.LogWarning("No partner endpoint registered for {PartnerId}; bundle {Id} backed off", bundle.PartnerId, bundle.Id);
            return;
        }

        Resource resource;
        try
        {
            resource = _parser.Deserialize<Resource>(bundle.FhirJson);
        }
        catch (Exception ex)
        {
            bundle.MarkAttemptFailed($"FHIR parse error: {ex.Message}", _timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.BackoffSeconds), _options.MaxAttempts);
            _logger.LogError(ex, "Failed to parse FHIR JSON for outbound bundle {Id}", bundle.Id);
            return;
        }

        try
        {
            var result = await endpoint.DeliverAsync(resource, cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                var deliveredAt = _timeProvider.GetUtcNow().UtcDateTime;
                bundle.MarkDelivered(deliveredAt);
                if (_compositionWriter is not null)
                {
                    try
                    {
                        await _compositionWriter
                            .WriteResourceAsync(bundle.PatientId, resource, bundle.PartnerId, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Composition write failed for bundle {Id}; continuing", bundle.Id);
                    }
                }
                await EmitDeliveredAsync(bundle, deliveredAt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var retryAt = _timeProvider.GetUtcNow().UtcDateTime.AddSeconds((double)_options.BackoffSeconds * bundle.Attempts);
                bundle.MarkAttemptFailed(result.FailureReason ?? $"HTTP {result.StatusCode}", retryAt, _options.MaxAttempts);
                if (bundle.Status == OutboundBundleStatus.Failed)
                    await EmitFailedAsync(bundle, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var retryAt = _timeProvider.GetUtcNow().UtcDateTime.AddSeconds((double)_options.BackoffSeconds * (bundle.Attempts + 1));
            bundle.MarkAttemptFailed(ex.Message, retryAt, _options.MaxAttempts);
            _logger.LogError(ex, "Delivery threw for bundle {Id}", bundle.Id);
            if (bundle.Status == OutboundBundleStatus.Failed)
                await EmitFailedAsync(bundle, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EmitDeliveredAsync(OutboundBundle bundle, DateTime deliveredAt, CancellationToken cancellationToken)
    {
        if (!_options.EmitDeliveryEvents || _transponderOutbox is null)
            return;
        var evt = new FhirResourceDeliveredIntegrationEvent(
            Guid.NewGuid(),
            deliveredAt,
            SchemaVersion: 1,
            bundle.Id,
            bundle.PatientId,
            bundle.ResourceType,
            bundle.LogicalId,
            bundle.PartnerId,
            deliveredAt);
        await EnqueueEventAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    private async Task EmitFailedAsync(OutboundBundle bundle, CancellationToken cancellationToken)
    {
        if (!_options.EmitDeliveryEvents || _transponderOutbox is null)
            return;
        var evt = new FhirResourceDeliveryFailedIntegrationEvent(
            Guid.NewGuid(),
            _timeProvider.GetUtcNow().UtcDateTime,
            SchemaVersion: 1,
            bundle.Id,
            bundle.PatientId,
            bundle.ResourceType,
            bundle.PartnerId,
            bundle.Attempts,
            bundle.LastFailureReason ?? "unknown");
        await EnqueueEventAsync(evt, cancellationToken).ConfigureAwait(false);
    }

    private async Task EnqueueEventAsync<T>(T evt, CancellationToken cancellationToken)
    {
        if (_transponderOutbox is null)
            return;
        var json = JsonSerializer.Serialize(evt);
        var envelope = new TransponderOutboxEnvelope(typeof(T).AssemblyQualifiedName!, json);
        await _transponderOutbox.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
