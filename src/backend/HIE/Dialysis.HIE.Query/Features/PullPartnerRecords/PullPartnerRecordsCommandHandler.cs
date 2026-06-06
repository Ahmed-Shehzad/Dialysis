using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.CQRS.Commands;
using Dialysis.HIE.Inbound.Ingestion;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Query.Features.PullPartnerRecords;

public sealed class PullPartnerRecordsCommandHandler : ICommandHandler<PullPartnerRecordsCommand, PartnerPullResult>
{
    private readonly IPartnerFhirQuery _query;
    private readonly InboundIngestionService _ingestion;

    public PullPartnerRecordsCommandHandler(IPartnerFhirQuery query, InboundIngestionService ingestion)
    {
        _query = query;
        _ingestion = ingestion;
    }

    public async Task<PartnerPullResult> HandleAsync(PullPartnerRecordsCommand request, CancellationToken cancellationToken)
    {
        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? TefcaPermittedPurposes.Treatment : request.Purpose;

        var resources = await _query
            .QueryAsync(request.PartnerId, request.Query, request.Subject, purpose, cancellationToken)
            .ConfigureAwait(false);
        if (resources.Count == 0)
            return new PartnerPullResult(0);

        // Pulled records land exactly like pushed-in records: same validation → consent → persist →
        // republish path. The partner id keys consent + idempotent storage on the inbound side.
        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = resources.Select(r => new Bundle.EntryComponent { Resource = r }).ToList(),
        };
        await _ingestion.IngestAsync(request.PartnerId.ToString(), bundle, purpose, cancellationToken).ConfigureAwait(false);

        return new PartnerPullResult(resources.Count);
    }
}
