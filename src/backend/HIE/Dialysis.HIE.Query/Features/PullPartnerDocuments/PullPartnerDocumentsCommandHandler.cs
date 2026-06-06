using Dialysis.BuildingBlocks.Fhir.Tefca;
using Dialysis.CQRS.Commands;
using Dialysis.HIE.Inbound.Ingestion;
using Dialysis.HIE.Query.Features.PullPartnerRecords;
using Dialysis.HIE.Query.Xca;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Query.Features.PullPartnerDocuments;

public sealed class PullPartnerDocumentsCommandHandler : ICommandHandler<PullPartnerDocumentsCommand, PartnerPullResult>
{
    private readonly IXcaQueryClient _xcaQuery;
    private readonly IXcaRetrieveClient _xcaRetrieve;
    private readonly InboundIngestionService _ingestion;

    public PullPartnerDocumentsCommandHandler(
        IXcaQueryClient xcaQuery, IXcaRetrieveClient xcaRetrieve, InboundIngestionService ingestion)
    {
        _xcaQuery = xcaQuery;
        _xcaRetrieve = xcaRetrieve;
        _ingestion = ingestion;
    }

    public async Task<PartnerPullResult> HandleAsync(PullPartnerDocumentsCommand request, CancellationToken cancellationToken)
    {
        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? TefcaPermittedPurposes.Treatment : request.Purpose;

        var documents = await _xcaQuery
            .QueryDocumentsAsync(request.PartnerId, request.PartnerPatientId, purpose, cancellationToken)
            .ConfigureAwait(false);
        if (documents.Count == 0)
            return new PartnerPullResult(0);

        // ITI-39 retrieve: inline each document's content so the persisted DocumentReference is
        // self-contained, then land them through the same ingestion path as any inbound resource.
        foreach (var document in documents)
        {
            var content = await _xcaRetrieve
                .RetrieveContentAsync(request.PartnerId, document, request.PartnerPatientId, purpose, cancellationToken)
                .ConfigureAwait(false);
            if (content is { Length: > 0 })
            {
                var attachment = (document.Content.FirstOrDefault()?.Attachment) ?? new Attachment();
                attachment.Data = content;
                if (document.Content.Count == 0)
                    document.Content.Add(new DocumentReference.ContentComponent { Attachment = attachment });
            }
        }

        var bundle = new Bundle
        {
            Type = Bundle.BundleType.Collection,
            Entry = documents.Select(d => new Bundle.EntryComponent { Resource = d }).ToList(),
        };
        await _ingestion.IngestAsync(request.PartnerId.ToString(), bundle, purpose, cancellationToken).ConfigureAwait(false);

        return new PartnerPullResult(documents.Count);
    }
}
