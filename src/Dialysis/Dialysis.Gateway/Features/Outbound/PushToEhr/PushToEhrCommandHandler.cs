using Dialysis.Gateway.Features.Audit;
using Dialysis.Gateway.Services;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Outbound.PushToEhr;

public sealed class PushToEhrCommandHandler : ICommandHandler<PushToEhrCommand, PushToEhrResult>
{
    private readonly IPatientDataService _patientData;
    private readonly IFhirBundleBuilder _bundleBuilder;
    private readonly IEhrOutboundClient _ehrClient;
    private readonly ITenantContext _tenantContext;
    private readonly ISender _sender;

    public PushToEhrCommandHandler(
        IPatientDataService patientData,
        IFhirBundleBuilder bundleBuilder,
        IEhrOutboundClient ehrClient,
        ITenantContext tenantContext,
        ISender sender)
    {
        _patientData = patientData;
        _bundleBuilder = bundleBuilder;
        _ehrClient = ehrClient;
        _tenantContext = tenantContext;
        _sender = sender;
    }

    public async Task<PushToEhrResult> HandleAsync(PushToEhrCommand request, CancellationToken cancellationToken = default)
    {
        if (!_ehrClient.IsConfigured)
            return new PushToEhrResult(false, null, "EHR outbound not configured. Set Integration:EhrFhirBaseUrl.", request.PatientId, 0);

        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);
        var data = await _patientData.GetAsync(tenantId, patientId, cancellationToken);
        if (data is null)
            return new PushToEhrResult(false, 404, "Patient not found.", request.PatientId, 0);

        var bundleJson = _bundleBuilder.BuildEhrPushTransactionBundle(data, request.BaseUrl);
        var result = await _ehrClient.PushPatientBundleAsync(request.PatientId, bundleJson, cancellationToken);

        var count = 1 + data.Conditions.Count + data.Episodes.Count + data.Sessions.Count * 2 + data.Observations.Count;
        if (result.Success)
        {
            await _sender.SendAsync(new RecordAuditCommand(
                Action: "PushToEhr",
                ResourceType: "Patient",
                Actor: "api",
                ResourceId: request.PatientId,
                PatientId: request.PatientId,
                Details: $"FHIR bundle, {count} resources"),
                cancellationToken);
        }

        return new PushToEhrResult(result.Success, result.StatusCode, result.ErrorMessage, request.PatientId, count);
    }
}
