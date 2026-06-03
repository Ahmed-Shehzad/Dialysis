using Dialysis.CQRS.Queries;
using Dialysis.HIE.Documents.Ports;

namespace Dialysis.HIE.Documents.Features.ListDocuments;

public sealed class ListDocumentsQueryHandler(IDocumentReferenceRepository repository)
    : IQueryHandler<ListDocumentsQuery, IReadOnlyList<DocumentRow>>
{
    public async Task<IReadOnlyList<DocumentRow>> HandleAsync(ListDocumentsQuery request, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(request.Take, 1, 500);
        var rows = await repository
            .ListAsync(request.PatientId, request.Kind, request.Status, request.Source, take, cancellationToken)
            .ConfigureAwait(false);
        return [.. rows.Select(r => new DocumentRow(
            r.Id,
            r.PatientId,
            r.Kind,
            r.Title,
            r.MimeType,
            r.LanguageCode,
            r.Status,
            r.Source,
            r.Size,
            r.CreatedAtUtc,
            r.Signatures.Count,
            r.HasAcroForms,
            r.HasJavascript))];
    }
}
