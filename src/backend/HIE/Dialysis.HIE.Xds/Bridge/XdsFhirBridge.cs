using Dialysis.HIE.Xds.Domain;
using Hl7.Fhir.Model;

namespace Dialysis.HIE.Xds.Bridge;

public interface IXdsToFhirMapper
{
    DocumentReference Map(DocumentEntry entry);
}

public interface IFhirToXdsMapper
{
    DocumentEntry Map(DocumentReference reference);
}

public sealed class DefaultXdsFhirBridge : IXdsToFhirMapper, IFhirToXdsMapper
{
    public DocumentReference Map(DocumentEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new DocumentReference
        {
            Id = entry.UniqueId,
            Status = DocumentReferenceStatus.Current,
            DocStatus = CompositionStatus.Final,
            Type = new CodeableConcept("urn:ihe:xds:type", entry.TypeCode),
            Category = [new CodeableConcept("urn:ihe:xds:class", entry.ClassCode)],
            Subject = new ResourceReference($"Patient/{entry.PatientId}"),
            Date = entry.CreationTime,
            SecurityLabel = [new CodeableConcept("urn:ihe:xds:confidentiality", entry.ConfidentialityCode)],
            Content =
            [
                new DocumentReference.ContentComponent
                {
                    Attachment = new Attachment
                    {
                        ContentType = entry.MimeType,
                        Title = entry.Title,
                        Size = (int?)entry.Size,
                    },
                    Format = new Coding("urn:ihe:xds:format", entry.FormatCode),
                },
            ],
        };
    }

    public DocumentEntry Map(DocumentReference reference)
    {
        ArgumentNullException.ThrowIfNull(reference);
        var content = reference.Content.FirstOrDefault();
        return new DocumentEntry(
            UniqueId: reference.Id ?? Guid.NewGuid().ToString(),
            PatientId: ExtractId(reference.Subject?.Reference) ?? string.Empty,
            MimeType: content?.Attachment?.ContentType ?? "application/octet-stream",
            FormatCode: content?.Format?.Code ?? "urn:ihe:iti:xds-sd:text:2008",
            ClassCode: reference.Category.FirstOrDefault()?.Coding.FirstOrDefault()?.Code ?? "Unknown",
            TypeCode: reference.Type?.Coding.FirstOrDefault()?.Code ?? "Unknown",
            ConfidentialityCode: reference.SecurityLabel.FirstOrDefault()?.Coding.FirstOrDefault()?.Code ?? "N",
            SourceOrgId: reference.Custodian?.Reference ?? string.Empty,
            CreationTime: reference.Date ?? DateTimeOffset.UtcNow,
            Title: content?.Attachment?.Title,
            RepositoryUniqueId: null,
            Size: content?.Attachment?.Size ?? 0);
    }

    private static string? ExtractId(string? reference)
    {
        if (string.IsNullOrEmpty(reference))
            return null;
        var slash = reference.IndexOf('/');
        return slash > 0 && slash + 1 < reference.Length ? reference[(slash + 1)..] : null;
    }
}
