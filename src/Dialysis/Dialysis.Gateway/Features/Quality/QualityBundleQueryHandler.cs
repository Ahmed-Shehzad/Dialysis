using Dialysis.Gateway.Features.Fhir;
using Dialysis.Gateway.Services;
using Dialysis.SharedKernel.Abstractions;

using Intercessor.Abstractions;

namespace Dialysis.Gateway.Features.Quality;

public sealed class QualityBundleQueryHandler : IQueryHandler<QualityBundleQuery, QualityBundleQueryResult>
{
    private readonly IQualityBundleService _qualityService;
    private readonly IDeidentificationService _deidentificationService;
    private readonly ITenantContext _tenantContext;

    public QualityBundleQueryHandler(IQualityBundleService qualityService, IDeidentificationService deidentificationService, ITenantContext tenantContext)
    {
        _qualityService = qualityService;
        _deidentificationService = deidentificationService;
        _tenantContext = tenantContext;
    }

    public async Task<QualityBundleQueryResult> HandleAsync(QualityBundleQuery request, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantContext.TenantId.Value;
        var result = await _qualityService.GetDeidentifiedBundleAsync(tenantId, request.From, request.To, request.Limit, cancellationToken);

        var bundle = new global::Hl7.Fhir.Model.Bundle
        {
            Type = global::Hl7.Fhir.Model.Bundle.BundleType.Collection,
            Total = result.SessionsInRange.Count,
            Meta = new global::Hl7.Fhir.Model.Meta
            {
                Security = [new global::Hl7.Fhir.Model.Coding("http://terminology.hl7.org/CodeSystem/v3-Confidentiality", "N", "normal")]
            }
        };

        var anonymizedId = 0;
        foreach (var session in result.SessionsInRange)
        {
            var fhirProc = FhirMappers.ToFhirProcedure(session, request.BaseUrl);
            fhirProc.Subject = new global::Hl7.Fhir.Model.ResourceReference($"urn:deidentified:patient:{++anonymizedId}");
            fhirProc.Id = $"proc-{session.Id}";
            bundle.Entry.Add(new global::Hl7.Fhir.Model.Bundle.EntryComponent
            {
                FullUrl = $"urn:uuid:proc-{session.Id}",
                Resource = fhirProc
            });
        }

        var json = FhirMappers.ToFhirJson(bundle);
        var deidentified = await _deidentificationService.DeidentifyAsync(json, cancellationToken);
        return new QualityBundleQueryResult(deidentified ?? json);
    }
}
