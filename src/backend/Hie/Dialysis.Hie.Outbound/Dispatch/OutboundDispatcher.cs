using System.Text.Json;
using Dialysis.BuildingBlocks.Transponder;
using Dialysis.Hie.Contracts.Integration;
using Dialysis.Hie.OpenEhr;
using Dialysis.Hie.Outbound.Domain;
using Dialysis.Hie.Outbound.Partners;
using Dialysis.Hie.Outbound.Ports;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.Hie.Outbound.Dispatch;

public sealed class OutboundDispatcher(
    IOutboundBundleStore store,
    IPartnerEndpointResolver resolver,
    CompositionWriter? compositionWriter,
    ITransponderOutbox? transponderOutbox,
    TimeProvider timeProvider,
    IOptions<OutboundOptions> options,
    ILogger<OutboundDispatcher> logger) : IOutboundDispatcher
{
    private static readonly FhirJsonParser _parser = new();
    private readonly OutboundOptions _options = options.Value;

    public async Task<int> TickAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var batch = await store.ClaimPendingAsync(_options.DispatchBatchSize, now, cancellationToken).ConfigureAwait(false);
        if (batch.Count == 0) return 0;

        var processed = 0;
        foreach (var bundle in batch)
        {
            await DeliverOneAsync(bundle, cancellationToken).ConfigureAwait(false);
            processed += 1;
        }

        await store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return processed;
    }

    private async Task DeliverOneAsync(OutboundBundle bundle, CancellationToken cancellationToken)
    {
        var endpoint = resolver.Resolve(bundle.PartnerId);
        if (endpoint is null)
        {
            var retryAt = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.BackoffSeconds);
            bundle.MarkAttemptFailed($"No partner endpoint registered for {bundle.PartnerId}", retryAt, _options.MaxAttempts);
            logger.LogWarning("No partner endpoint registered for {PartnerId}; bundle {Id} backed off", bundle.PartnerId, bundle.Id);
            return;
        }

        Resource resource;
        try
        {
            resource = _parser.Parse<Resource>(bundle.FhirJson);
        }
        catch (Exception ex)
        {
            bundle.MarkAttemptFailed($"FHIR parse error: {ex.Message}", timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.BackoffSeconds), _options.MaxAttempts);
            logger.LogError(ex, "Failed to parse FHIR JSON for outbound bundle {Id}", bundle.Id);
            return;
        }

        try
        {
            var result = await endpoint.DeliverAsync(resource, cancellationToken).ConfigureAwait(false);
            if (result.Succeeded)
            {
                var deliveredAt = timeProvider.GetUtcNow().UtcDateTime;
                bundle.MarkDelivered(deliveredAt);
                if (compositionWriter is not null)
                {
                    try
                    {
                        await compositionWriter
                            .WriteResourceAsync(bundle.PatientId, resource, bundle.PartnerId, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Composition write failed for bundle {Id}; continuing", bundle.Id);
                    }
                }
                await EmitDeliveredAsync(bundle, deliveredAt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                var retryAt = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.BackoffSeconds * bundle.Attempts);
                bundle.MarkAttemptFailed(result.FailureReason ?? $"HTTP {result.StatusCode}", retryAt, _options.MaxAttempts);
                if (bundle.Status == OutboundBundleStatus.Failed)
                    await EmitFailedAsync(bundle, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            var retryAt = timeProvider.GetUtcNow().UtcDateTime.AddSeconds(_options.BackoffSeconds * (bundle.Attempts + 1));
            bundle.MarkAttemptFailed(ex.Message, retryAt, _options.MaxAttempts);
            logger.LogError(ex, "Delivery threw for bundle {Id}", bundle.Id);
            if (bundle.Status == OutboundBundleStatus.Failed)
                await EmitFailedAsync(bundle, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task EmitDeliveredAsync(OutboundBundle bundle, DateTime deliveredAt, CancellationToken cancellationToken)
    {
        if (!_options.EmitDeliveryEvents || transponderOutbox is null) return;
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
        if (!_options.EmitDeliveryEvents || transponderOutbox is null) return;
        var evt = new FhirResourceDeliveryFailedIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow().UtcDateTime,
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
        if (transponderOutbox is null) return;
        var json = JsonSerializer.Serialize(evt);
        var envelope = new TransponderOutboxEnvelope(typeof(T).AssemblyQualifiedName!, json);
        await transponderOutbox.EnqueueAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
