using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.CQRS.Commands;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Inbound.Mpi;
using Dialysis.HIE.Query.Xca;
using Hl7.Fhir.Model;
using Task = System.Threading.Tasks.Task;

namespace Dialysis.HIE.Query.Features.PullOutsideRecords;

public sealed class PullOutsideRecordsCommandHandler : ICommandHandler<PullOutsideRecordsCommand, OutsideRecordsResult>
{
    private readonly IPartnerPatientDiscovery _discovery;
    private readonly IPartnerFhirQuery _query;
    private readonly IXcaQueryClient _xcaQuery;
    private readonly IXcaRetrieveClient _xcaRetrieve;
    private readonly InboundIngestionService _ingestion;

    public PullOutsideRecordsCommandHandler(
        IPartnerPatientDiscovery discovery,
        IPartnerFhirQuery query,
        IXcaQueryClient xcaQuery,
        IXcaRetrieveClient xcaRetrieve,
        InboundIngestionService ingestion)
    {
        _discovery = discovery;
        _query = query;
        _xcaQuery = xcaQuery;
        _xcaRetrieve = xcaRetrieve;
        _ingestion = ingestion;
    }

    public async Task<OutsideRecordsResult> HandleAsync(PullOutsideRecordsCommand request, CancellationToken cancellationToken)
    {
        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? TefcaPermittedPurposes.Treatment : request.Purpose;

        // 1) Resolve the partner-side patient id (discovery when not supplied).
        var candidates = 0;
        var partnerPatientId = request.PartnerPatientId;
        if (string.IsNullOrWhiteSpace(partnerPatientId))
        {
            var criteria = new PatientMatchCriteria(request.Mrn, request.Family, request.Given, request.BirthDate, SexAtBirthCode: null);
            var subject = request.Mrn ?? "patient-discovery";
            var discovered = await _discovery.DiscoverAsync(request.PartnerId, criteria, subject, purpose, cancellationToken).ConfigureAwait(false);
            candidates = discovered.Count;
            partnerPatientId = discovered.FirstOrDefault()?.PartnerPatientId;
            if (string.IsNullOrWhiteSpace(partnerPatientId))
                return new OutsideRecordsResult(candidates, null, 0, 0);
        }

        // 2) Pull the patient's records ($everything) and documents (XCA), landing both via ingestion.
        var records = await _query
            .QueryAsync(request.PartnerId, $"Patient/{Uri.EscapeDataString(partnerPatientId)}/$everything", partnerPatientId, purpose, cancellationToken)
            .ConfigureAwait(false);
        await IngestAsync(request.PartnerId, records, purpose, cancellationToken).ConfigureAwait(false);

        var documents = await _xcaQuery.QueryDocumentsAsync(request.PartnerId, partnerPatientId, purpose, cancellationToken).ConfigureAwait(false);
        foreach (var document in documents)
        {
            var content = await _xcaRetrieve.RetrieveContentAsync(request.PartnerId, document, partnerPatientId, purpose, cancellationToken).ConfigureAwait(false);
            if (content is { Length: > 0 })
            {
                var attachment = document.Content.FirstOrDefault()?.Attachment ?? new Attachment();
                attachment.Data = content;
                if (document.Content.Count == 0)
                    document.Content.Add(new DocumentReference.ContentComponent { Attachment = attachment });
            }
        }
        await IngestAsync(request.PartnerId, documents, purpose, cancellationToken).ConfigureAwait(false);

        return new OutsideRecordsResult(candidates, partnerPatientId, records.Count, documents.Count);
    }

    private async Task IngestAsync(Guid partnerId, IReadOnlyList<Resource> resources, string purpose, CancellationToken cancellationToken)
    {
        if (resources.Count == 0)
            return;
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = resources.Select(r => new Bundle.EntryComponent { Resource = r }).ToList(),
        };
        await _ingestion.IngestAsync(partnerId.ToString(), bundle, purpose, cancellationToken).ConfigureAwait(false);
    }
}
