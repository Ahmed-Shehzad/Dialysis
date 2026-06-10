using Hl7.Fhir.Model;
using FhirStatus = Hl7.Fhir.Model.DocumentReferenceStatus;
using DomainStatus = Dialysis.HIE.Documents.Domain.DocumentReferenceStatus;

namespace Dialysis.HIE.Documents.Fhir;

/// <summary>
/// Maps the platform's <see cref="DocumentReference"/> aggregate to and from a FHIR R4
/// <c>DocumentReference</c> resource. The binary content lives in the shared
/// <c>IDocumentBlobStore</c> — the FHIR resource carries the storage-ref via
/// <c>content.attachment.url</c> rather than embedding bytes.
/// </summary>
public static class DocumentReferenceMapper
{
    public static DocumentReference ToFhir(Domain.DocumentReference document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return new DocumentReference
        {
            Id = document.Id.ToString("N"),
            Status = document.Status switch
            {
                DomainStatus.Current => FhirStatus.Current,
                DomainStatus.Superseded => FhirStatus.Superseded,
                DomainStatus.EnteredInError => FhirStatus.EnteredInError,
                _ => FhirStatus.Current,
            },
            Type = new CodeableConcept("http://terminology.dialysis.local/document-kind", document.Kind, document.Title),
            Category = document.Category is null
                ? null
                : [new CodeableConcept("http://terminology.dialysis.local/document-category", document.Category)],
            Subject = new ResourceReference($"Patient/{document.PatientId:N}"),
            Date = new DateTimeOffset(DateTime.SpecifyKind(document.CreatedAtUtc, DateTimeKind.Utc)),
            Content =
            [
                new DocumentReference.ContentComponent
                {
                    Attachment = new Attachment
                    {
                        ContentType = document.MimeType,
                        Language = document.LanguageCode,
                        Url = document.StorageRef,
                        Title = document.Title,
                        Size = (int?)Math.Min(document.Size, int.MaxValue),
                        Hash = HexToBytes(document.ContentHash),
                    },
                },
            ],
        };
    }

    private static byte[]? HexToBytes(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try
        {
            return Convert.FromHexString(hex);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
