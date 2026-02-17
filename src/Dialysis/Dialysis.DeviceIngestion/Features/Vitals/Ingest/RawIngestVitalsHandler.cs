using System.Text.Json;

using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

using Microsoft.Extensions.Logging;

namespace Dialysis.DeviceIngestion.Features.Vitals.Ingest;

/// <summary>
/// Handles raw device ingest via IDeviceMessageAdapter. Phase 1.1.4.
/// </summary>
public sealed class RawIngestVitalsHandler : ICommandHandler<RawIngestVitalsCommand, RawIngestVitalsResult>
{
    private readonly IEnumerable<IDeviceMessageAdapter> _adapters;
    private readonly ISender _sender;
    private readonly ILogger<RawIngestVitalsHandler> _logger;

    public RawIngestVitalsHandler(
        IEnumerable<IDeviceMessageAdapter> adapters,
        ISender sender,
        ILogger<RawIngestVitalsHandler> logger)
    {
        _adapters = adapters;
        _sender = sender;
        _logger = logger;
    }

    public async Task<RawIngestVitalsResult> HandleAsync(RawIngestVitalsCommand request, CancellationToken cancellationToken = default)
    {
        var adapter = _adapters.FirstOrDefault(a =>
            string.Equals(a.AdapterId, request.AdapterId, StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
            return new RawIngestVitalsResult(false, null, $"Adapter '{request.AdapterId}' not found.");

        if (!adapter.CanHandle(request.RawPayload))
            return new RawIngestVitalsResult(false, null, $"Adapter '{request.AdapterId}' cannot handle this payload.");

        var json = await adapter.TransformToVitalsJsonAsync(request.RawPayload, cancellationToken);
        if (string.IsNullOrWhiteSpace(json))
            return new RawIngestVitalsResult(false, null, "Adapter transformation returned empty.");

        IngestVitalsRequest? vitalsRequest;
        try
        {
            vitalsRequest = JsonSerializer.Deserialize<IngestVitalsRequest>(json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Adapter returned invalid JSON");
            return new RawIngestVitalsResult(false, null, "Adapter returned invalid JSON.");
        }

        if (vitalsRequest is null || string.IsNullOrWhiteSpace(vitalsRequest.PatientId))
            return new RawIngestVitalsResult(false, null, "PatientId is required.");

        var result = await IngestVitalsEndpoint.HandleAsync(vitalsRequest, _sender, cancellationToken);
        return new RawIngestVitalsResult(true, result.ObservationId, null);
    }
}
