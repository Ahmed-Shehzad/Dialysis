using Dialysis.DeviceIngestion.Services;
using Dialysis.Tenancy;
using Intercessor.Abstractions;

namespace Dialysis.DeviceIngestion.Features.IngestVitals;

public sealed class IngestVitalsHandler : ICommandHandler<IngestVitalsCommand, IngestVitalsResult>
{
    private readonly IFhirObservationWriter _writer;
    private readonly ITenantContext _tenantContext;

    public IngestVitalsHandler(IFhirObservationWriter writer, ITenantContext tenantContext)
    {
        _writer = writer;
        _tenantContext = tenantContext;
    }

    public async Task<IngestVitalsResult> HandleAsync(IngestVitalsCommand request, CancellationToken cancellationToken = default)
    {
        var observationIds = await _writer.WriteObservationsAsync(
            _tenantContext.TenantId,
            request.PatientId,
            request.EncounterId,
            request.DeviceId,
            request.Readings,
            cancellationToken);

        return new IngestVitalsResult { ObservationIds = observationIds };
    }
}

public sealed record IngestVitalsResult
{
    public required IReadOnlyList<string> ObservationIds { get; init; }
}
