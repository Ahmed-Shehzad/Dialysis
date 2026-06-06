using Hl7.Fhir.Model;

namespace Dialysis.HIE.Query.Xca;

/// <summary>
/// XCA document query + retrieve over the Phase-3a FHIR query client, so it inherits partner
/// resolution and purpose-scoped IAS JWT auth. ITI-38 maps to a <c>DocumentReference</c> search;
/// ITI-39 maps to reading the referenced <c>Binary</c> (or using the inline attachment).
/// </summary>
public sealed class XcaDocumentClient : IXcaQueryClient, IXcaRetrieveClient
{
    private readonly IPartnerFhirQuery _query;
    public XcaDocumentClient(IPartnerFhirQuery query) => _query = query;

    public async Task<IReadOnlyList<DocumentReference>> QueryDocumentsAsync(
        Guid partnerId, string partnerPatientId, string purposeOfUse, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(partnerPatientId);
        var resources = await _query
            .QueryAsync(partnerId, $"DocumentReference?patient={Uri.EscapeDataString(partnerPatientId)}", partnerPatientId, purposeOfUse, cancellationToken)
            .ConfigureAwait(false);
        return resources.OfType<DocumentReference>().ToList();
    }

    public async Task<byte[]?> RetrieveContentAsync(
        Guid partnerId, DocumentReference document, string subject, string purposeOfUse, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        var attachment = document.Content.FirstOrDefault()?.Attachment;
        if (attachment is null)
            return null;

        // ITI-39: inline content is already retrieved; otherwise dereference the Binary URL.
        if (attachment.Data is { Length: > 0 } inline)
            return inline;
        if (string.IsNullOrWhiteSpace(attachment.Url))
            return null;

        var relative = ToRelativeReference(attachment.Url);
        var resources = await _query
            .QueryAsync(partnerId, relative, subject, purposeOfUse, cancellationToken)
            .ConfigureAwait(false);
        return resources.OfType<Binary>().FirstOrDefault()?.Data;
    }

    // Partner base URLs vary; keep only the trailing "{Type}/{id}" so the query client resolves it
    // against the partner's FHIR base.
    private static string ToRelativeReference(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute))
        {
            var segments = absolute.Segments.Select(s => s.Trim('/')).Where(s => s.Length > 0).ToArray();
            return segments.Length >= 2 ? $"{segments[^2]}/{segments[^1]}" : absolute.AbsolutePath.TrimStart('/');
        }
        return url.TrimStart('/');
    }
}
