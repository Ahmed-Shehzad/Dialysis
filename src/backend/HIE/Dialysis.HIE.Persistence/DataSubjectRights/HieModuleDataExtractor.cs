using Dialysis.BuildingBlocks.DataProtection.DataSubjectRights;
using Dialysis.HIE.Documents.Domain;
using Microsoft.EntityFrameworkCore;

namespace Dialysis.HIE.Persistence.DataSubjectRights;

/// <summary>
/// HIE contribution to the GDPR Art. 15 / 20 export pipeline. Structurally mirrors
/// <see cref="Erasure.HiePatientEraser"/>: walks the same patient-keyed tables the eraser touches,
/// but reads them into <see cref="DataSubjectResource"/> entries instead of deleting:
/// <list type="bullet">
///   <item><c>hie_documents.DocumentReferences</c> — document <em>metadata</em> only (title, kind,
///     hash, signatures); the binary blob is downloadable through the documents API, not inlined
///     into the export bundle. Purged tombstones (<c>EnteredInError</c>) are excluded;</item>
///   <item><c>hie_consent.Consents</c> — the patient's disclosure consents;</item>
///   <item><c>hie_outbound.OutboundBundles</c> — exported with the stored <c>FhirJson</c> as the
///     resource body, so this slice of the bundle is genuinely FHIR;</item>
///   <item><c>hie_openehr.Compositions</c> — openEHR projections (payload inline).</item>
/// </list>
/// </summary>
public sealed class HieModuleDataExtractor : IModuleDataExtractor
{
    private readonly HieDbContext _ctx;

    public HieModuleDataExtractor(HieDbContext ctx) => _ctx = ctx;

    public string ModuleSlug => "hie";

    public async Task<IReadOnlyList<DataSubjectResource>> ExtractAsync(
        Guid patientId, CancellationToken cancellationToken)
    {
        var resources = new List<DataSubjectResource>();

        var documents = await _ctx.DocumentReferences
            .AsNoTracking()
            .Where(d => d.PatientId == patientId && d.Status != DocumentReferenceStatus.EnteredInError)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var document in documents)
        {
            resources.Add(new DataSubjectResource(
                "DocumentReference", document.Id.ToString(), DataSubjectExportJson.Serialize(document)));
        }

        var consents = await _ctx.Consents
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var consent in consents)
        {
            resources.Add(new DataSubjectResource(
                "Consent", consent.Id.ToString(), DataSubjectExportJson.Serialize(consent)));
        }

        // Outbound bundles already persist the mapped FHIR resource — surface it verbatim so the
        // export stays "FHIR JSON form when possible".
        var bundles = await _ctx.OutboundBundles
            .AsNoTracking()
            .Where(b => b.PatientId == patientId)
            .Select(b => new { b.Id, b.ResourceType, b.FhirJson })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var bundle in bundles)
            resources.Add(new DataSubjectResource(bundle.ResourceType, bundle.Id.ToString(), bundle.FhirJson));

        var compositions = await _ctx.Compositions
            .AsNoTracking()
            .Where(c => c.PatientId == patientId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var composition in compositions)
        {
            resources.Add(new DataSubjectResource(
                "OpenEhrComposition", composition.Id.ToString(), DataSubjectExportJson.Serialize(composition)));
        }

        return resources;
    }
}
