using Dialysis.Gateway.Services;
using Dialysis.SharedKernel.Abstractions;
using Dialysis.SharedKernel.ValueObjects;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Fhir.PatientEverything;

public sealed class PatientEverythingQueryHandler : IQueryHandler<PatientEverythingQuery, PatientEverythingResult?>
{
    private readonly IPatientDataService _patientData;
    private readonly IFhirBundleBuilder _bundleBuilder;
    private readonly ITenantContext _tenantContext;

    public PatientEverythingQueryHandler(
        IPatientDataService patientData,
        IFhirBundleBuilder bundleBuilder,
        ITenantContext tenantContext)
    {
        _patientData = patientData;
        _bundleBuilder = bundleBuilder;
        _tenantContext = tenantContext;
    }

    public async Task<PatientEverythingResult?> HandleAsync(PatientEverythingQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId;
        var patientId = new PatientId(request.PatientId);
        var data = await _patientData.GetAsync(tenantId, patientId, cancellationToken);
        if (data is null)
            return null;

        var json = _bundleBuilder.BuildPatientEverythingBundle(data, request.BaseUrl);
        return new PatientEverythingResult(json);
    }
}
